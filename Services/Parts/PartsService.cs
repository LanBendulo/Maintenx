using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IT15_Project.Services.Parts
{
    /// <summary>
    /// Service for managing parts usage with staged workflow
    /// Separates staging from inventory consumption
    /// </summary>
    public class PartsService : IPartsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PartsService> _logger;

        public PartsService(
            ApplicationDbContext context,
            ILogger<PartsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Stage a part for use in a work order
        /// Validates availability but does NOT deduct inventory
        /// </summary>
        public async Task<(bool Success, string Message, WorkOrderPart? Part)> StagePartUsageAsync(
            int workOrderId,
            int partId,
            int quantityUsed,
            int personnelId,
            int companyId)
        {
            try
            {
                // Validate work order exists and belongs to company
                var workOrder = await _context.WorkOrders
                    .FirstOrDefaultAsync(wo => wo.WorkOrderId == workOrderId && wo.CompanyId == companyId);

                if (workOrder == null)
                {
                    return (false, "Work order not found.", null);
                }

                // Validate work order status allows parts staging
                if (workOrder.Status == WorkOrderStatuses.Completed || workOrder.Status == WorkOrderStatuses.Cancelled)
                {
                    return (false, "Cannot add parts to completed or cancelled work orders.", null);
                }

                // Validate part exists and belongs to company
                var part = await _context.Parts
                    .FirstOrDefaultAsync(p => p.PartId == partId && p.CompanyId == companyId && p.IsActive);

                if (part == null)
                {
                    return (false, "Part not found or inactive.", null);
                }

                // Validate stock availability (including already staged parts)
                var validation = await ValidatePartAvailabilityAsync(partId, quantityUsed, companyId);
                if (!validation.CanStage)
                {
                    return (false, validation.Message, null);
                }

                // Check if part already staged for this work order
                var existingUsage = await _context.WorkOrderParts
                    .FirstOrDefaultAsync(wop => 
                        wop.WorkOrderId == workOrderId && 
                        wop.PartId == partId && 
                        wop.UsageStatus == PartUsageStatuses.Pending);

                if (existingUsage != null)
                {
                    // Update existing staged quantity
                    existingUsage.QuantityUsed += quantityUsed;
                    existingUsage.TotalCost = existingUsage.QuantityUsed * (part.UnitCost ?? 0);
                    existingUsage.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Updated staged part usage: WO#{WorkOrderId}, Part#{PartId}, Quantity: {Quantity}, Personnel: {PersonnelId}",
                        workOrderId, partId, existingUsage.QuantityUsed, personnelId);

                    return (true, "Part quantity updated successfully.", existingUsage);
                }

                // Create new staged part usage
                var workOrderPart = new WorkOrderPart
                {
                    CompanyId = companyId,
                    WorkOrderId = workOrderId,
                    PartId = partId,
                    QuantityUsed = quantityUsed,
                    UnitCost = part.UnitCost,
                    TotalCost = quantityUsed * (part.UnitCost ?? 0),
                    UsageStatus = PartUsageStatuses.Pending,
                    AddedByPersonnelId = personnelId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.WorkOrderParts.Add(workOrderPart);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Staged part usage: WO#{WorkOrderId}, Part#{PartId}, Quantity: {Quantity}, Personnel: {PersonnelId}",
                    workOrderId, partId, quantityUsed, personnelId);

                return (true, "Part staged successfully.", workOrderPart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error staging part usage for WO#{WorkOrderId}", workOrderId);
                return (false, "An error occurred while staging the part.", null);
            }
        }

        /// <summary>
        /// Remove staged part usage
        /// Only allowed for Pending status
        /// </summary>
        public async Task<(bool Success, string Message)> RemoveStagedPartAsync(
            int workOrderPartId,
            int personnelId,
            int companyId)
        {
            try
            {
                var workOrderPart = await _context.WorkOrderParts
                    .Include(wop => wop.WorkOrder)
                    .FirstOrDefaultAsync(wop => 
                        wop.Id == workOrderPartId && 
                        wop.CompanyId == companyId);

                if (workOrderPart == null)
                {
                    return (false, "Part usage not found.");
                }

                // Validate status allows removal
                if (workOrderPart.UsageStatus != PartUsageStatuses.Pending)
                {
                    return (false, $"Cannot remove part with status '{workOrderPart.UsageStatus}'.");
                }

                // Validate work order status
                if (workOrderPart.WorkOrder?.Status == WorkOrderStatuses.Completed || 
                    workOrderPart.WorkOrder?.Status == WorkOrderStatuses.Cancelled)
                {
                    return (false, "Cannot remove parts from completed or cancelled work orders.");
                }

                _context.WorkOrderParts.Remove(workOrderPart);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Removed staged part: WO#{WorkOrderId}, Part#{PartId}, Personnel: {PersonnelId}",
                    workOrderPart.WorkOrderId, workOrderPart.PartId, personnelId);

                return (true, "Part removed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing staged part #{WorkOrderPartId}", workOrderPartId);
                return (false, "An error occurred while removing the part.");
            }
        }

        /// <summary>
        /// Update staged part quantity
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateStagedPartQuantityAsync(
            int workOrderPartId,
            int newQuantity,
            int personnelId,
            int companyId)
        {
            try
            {
                if (newQuantity <= 0)
                {
                    return (false, "Quantity must be greater than zero.");
                }

                var workOrderPart = await _context.WorkOrderParts
                    .Include(wop => wop.WorkOrder)
                    .Include(wop => wop.Part)
                    .FirstOrDefaultAsync(wop => 
                        wop.Id == workOrderPartId && 
                        wop.CompanyId == companyId);

                if (workOrderPart == null)
                {
                    return (false, "Part usage not found.");
                }

                // Validate status allows update
                if (workOrderPart.UsageStatus != PartUsageStatuses.Pending)
                {
                    return (false, $"Cannot update part with status '{workOrderPart.UsageStatus}'.");
                }

                // Validate work order status
                if (workOrderPart.WorkOrder?.Status == WorkOrderStatuses.Completed || 
                    workOrderPart.WorkOrder?.Status == WorkOrderStatuses.Cancelled)
                {
                    return (false, "Cannot update parts in completed or cancelled work orders.");
                }

                // Validate stock availability for new quantity
                var validation = await ValidatePartAvailabilityAsync(
                    workOrderPart.PartId, 
                    newQuantity, 
                    companyId,
                    excludeWorkOrderPartId: workOrderPartId);

                if (!validation.CanStage)
                {
                    return (false, validation.Message);
                }

                // Update quantity and cost
                workOrderPart.QuantityUsed = newQuantity;
                workOrderPart.TotalCost = newQuantity * (workOrderPart.Part?.UnitCost ?? 0);
                workOrderPart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated staged part quantity: WO#{WorkOrderId}, Part#{PartId}, NewQuantity: {Quantity}",
                    workOrderPart.WorkOrderId, workOrderPart.PartId, newQuantity);

                return (true, "Part quantity updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staged part quantity #{WorkOrderPartId}", workOrderPartId);
                return (false, "An error occurred while updating the part quantity.");
            }
        }

        /// <summary>
        /// Get all staged parts for a work order
        /// </summary>
        public async Task<List<WorkOrderPart>> GetStagedPartsAsync(int workOrderId, int companyId)
        {
            return await _context.WorkOrderParts
                .Include(wop => wop.Part)
                .Include(wop => wop.AddedByPersonnel)
                .Where(wop => 
                    wop.WorkOrderId == workOrderId && 
                    wop.CompanyId == companyId)
                .OrderBy(wop => wop.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Validate part availability without modifying inventory
        /// Considers already staged parts across all work orders
        /// </summary>
        public async Task<(bool CanStage, string Message, int AvailableStock)> ValidatePartAvailabilityAsync(
            int partId,
            int requestedQuantity,
            int companyId,
            int? excludeWorkOrderPartId = null)
        {
            var part = await _context.Parts
                .FirstOrDefaultAsync(p => p.PartId == partId && p.CompanyId == companyId);

            if (part == null)
            {
                return (false, "Part not found.", 0);
            }

            if (!part.IsActive)
            {
                return (false, "Part is inactive.", 0);
            }

            // Calculate total staged quantity (Pending status only)
            var stagedQuery = _context.WorkOrderParts
                .Where(wop => 
                    wop.PartId == partId && 
                    wop.CompanyId == companyId && 
                    wop.UsageStatus == PartUsageStatuses.Pending);

            if (excludeWorkOrderPartId.HasValue)
            {
                stagedQuery = stagedQuery.Where(wop => wop.Id != excludeWorkOrderPartId.Value);
            }

            var totalStaged = await stagedQuery.SumAsync(wop => wop.QuantityUsed);

            // Available = Current Stock - Already Staged
            var availableStock = part.Quantity - totalStaged;

            if (availableStock < requestedQuantity)
            {
                return (false, 
                    $"Insufficient stock. Available: {availableStock}, Requested: {requestedQuantity}", 
                    availableStock);
            }

            return (true, "Stock available.", availableStock);
        }

        /// <summary>
        /// Consume staged parts - finalize inventory deduction with transactional safety
        /// Called during WO completion
        /// Creates immutable inventory movement records for audit trail
        /// </summary>
        public async Task<(bool Success, string Message)> ConsumeWorkOrderPartsAsync(
            int workOrderId,
            string userId,
            int companyId)
        {
            try
            {
                // Use execution strategy for transaction with retry support
                var strategy = _context.Database.CreateExecutionStrategy();
                
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    
                    try
                    {
                        // Get all pending staged parts for this work order
                        var stagedParts = await _context.WorkOrderParts
                            .Include(wop => wop.Part)
                            .Where(wop => 
                                wop.WorkOrderId == workOrderId && 
                                wop.CompanyId == companyId && 
                                wop.UsageStatus == PartUsageStatuses.Pending)
                            .ToListAsync();

                        if (!stagedParts.Any())
                        {
                            await transaction.CommitAsync();
                            return (true, "No parts to consume.");
                        }

                        var movementRecords = new List<InventoryMovement>();

                        // Process each staged part with transactional safety
                        foreach (var stagedPart in stagedParts)
                        {
                            if (stagedPart.Part == null)
                            {
                                _logger.LogWarning(
                                    "Skipping WorkOrderPart #{Id} - Part not loaded",
                                    stagedPart.Id);
                                continue;
                            }

                            // NEGATIVE STOCK PREVENTION
                            if (stagedPart.Part.Quantity < stagedPart.QuantityUsed)
                            {
                                await transaction.RollbackAsync();
                                
                                _logger.LogError(
                                    "Insufficient stock for Part #{PartId} '{PartName}'. Available: {Available}, Required: {Required}",
                                    stagedPart.PartId,
                                    stagedPart.Part.PartName,
                                    stagedPart.Part.Quantity,
                                    stagedPart.QuantityUsed);

                                return (false, 
                                    $"Insufficient stock for part '{stagedPart.Part.PartName}'. " +
                                    $"Available: {stagedPart.Part.Quantity}, Required: {stagedPart.QuantityUsed}");
                            }

                            // Capture before state
                            var previousQuantity = stagedPart.Part.Quantity;
                            var unitCost = stagedPart.Part.UnitCost ?? 0;

                            // Deduct from inventory
                            stagedPart.Part.Quantity -= stagedPart.QuantityUsed;
                            stagedPart.Part.UpdatedAt = DateTime.UtcNow;

                            var newQuantity = stagedPart.Part.Quantity;

                            // Mark WorkOrderPart as consumed
                            stagedPart.UsageStatus = PartUsageStatuses.Consumed;
                            stagedPart.ConsumedAt = DateTime.UtcNow;
                            stagedPart.ApprovedByUserId = userId;
                            stagedPart.UpdatedAt = DateTime.UtcNow;

                            // Create immutable inventory movement record
                            var movement = new InventoryMovement
                            {
                                CompanyId = companyId,
                                PartId = stagedPart.PartId,
                                QuantityChanged = -stagedPart.QuantityUsed, // Negative for consumption
                                PreviousQuantity = previousQuantity,
                                NewQuantity = newQuantity,
                                MovementType = InventoryMovementTypes.Consumption,
                                WorkOrderId = workOrderId,
                                WorkOrderPartId = stagedPart.Id,
                                PerformedByUserId = userId,
                                UnitCostSnapshot = unitCost,
                                TotalCost = stagedPart.QuantityUsed * unitCost,
                                Notes = $"Consumed for Work Order #{workOrderId}",
                                CreatedAt = DateTime.UtcNow
                            };

                            movementRecords.Add(movement);

                            _logger.LogInformation(
                                "Consuming Part #{PartId} '{PartName}': {Quantity} units, Stock: {Previous} → {New}",
                                stagedPart.PartId,
                                stagedPart.Part.PartName,
                                stagedPart.QuantityUsed,
                                previousQuantity,
                                newQuantity);
                        }

                        // Add all movement records
                        if (movementRecords.Any())
                        {
                            await _context.InventoryMovements.AddRangeAsync(movementRecords);
                        }

                        // Commit all changes atomically
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation(
                            "Successfully consumed {Count} parts for WO#{WorkOrderId} by User: {UserId}. Created {MovementCount} inventory movement records.",
                            stagedParts.Count,
                            workOrderId,
                            userId,
                            movementRecords.Count);

                        return (true, $"Successfully consumed {movementRecords.Count} parts.");
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error consuming parts for WO#{WorkOrderId}. Transaction rolled back.",
                    workOrderId);
                
                return (false, "An error occurred while consuming parts. No inventory changes were made.");
            }
        }

        /// <summary>
        /// Get available parts for selection
        /// Hides cost for technicians
        /// </summary>
        public async Task<List<Part>> GetAvailablePartsAsync(int companyId, bool includeCost = false)
        {
            var query = _context.Parts
                .Where(p => p.CompanyId == companyId && p.IsActive && p.Quantity > 0);

            var parts = await query.ToListAsync();

            // Hide cost information if not authorized
            if (!includeCost)
            {
                foreach (var part in parts)
                {
                    part.UnitCost = null;
                }
            }

            return parts.OrderBy(p => p.PartName).ToList();
        }
    }
}
