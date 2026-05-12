using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Services;
using System.Security.Claims;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// Parts Inventory Management Controller
    /// Owner/Admin only - manages spare parts inventory
    /// MULTI-TENANT: All queries filtered by CompanyId
    /// </summary>
    [Authorize(Roles = "Owner,Admin")]
    [Route("admin/parts")]
    public class PartsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<PartsController> _logger;

        public PartsController(
            ApplicationDbContext context, 
            ITenantService tenantService,
            ILogger<PartsController> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        /// <summary>
        /// Parts Inventory Index - List all parts with filters
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index(string search = "", string status = "all", bool lowStock = false)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Base query with tenant filtering
            var query = _context.Parts
                .Where(p => p.CompanyId == companyId)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p => 
                    p.PartName.ToLower().Contains(search) ||
                    (p.PartNumber != null && p.PartNumber.ToLower().Contains(search)) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)));
            }

            if (status == "active")
            {
                query = query.Where(p => p.IsActive);
            }
            else if (status == "inactive")
            {
                query = query.Where(p => !p.IsActive);
            }

            if (lowStock)
            {
                query = query.Where(p => p.ReorderLevel.HasValue && p.Quantity <= p.ReorderLevel.Value);
            }

            var parts = await query
                .OrderBy(p => p.PartName)
                .ToListAsync();

            // Calculate summary metrics
            var allParts = await _context.Parts
                .Where(p => p.CompanyId == companyId)
                .ToListAsync();

            ViewBag.TotalParts = allParts.Count;
            ViewBag.ActiveParts = allParts.Count(p => p.IsActive);
            ViewBag.InactiveParts = allParts.Count(p => !p.IsActive);
            ViewBag.LowStockParts = allParts.Count(p => p.ReorderLevel.HasValue && p.Quantity <= p.ReorderLevel.Value);

            ViewBag.SearchFilter = search;
            ViewBag.StatusFilter = status;
            ViewBag.LowStockFilter = lowStock;

            ViewData["Active"] = "Parts";
            return View(parts);
        }

        /// <summary>
        /// Get part details by ID (JSON)
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetPart(int id)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var part = await _context.Parts
                .Where(p => p.PartId == id && p.CompanyId == companyId)
                .FirstOrDefaultAsync();

            if (part == null)
            {
                return NotFound(new { success = false, message = "Part not found." });
            }

            var result = new
            {
                partId = part.PartId,
                partName = part.PartName,
                partNumber = part.PartNumber,
                description = part.Description,
                quantity = part.Quantity,
                unitCost = part.UnitCost,
                reorderLevel = part.ReorderLevel,
                location = part.Location,
                isActive = part.IsActive,
                createdAt = part.CreatedAt,
                updatedAt = part.UpdatedAt
            };

            return Ok(result);
        }

        /// <summary>
        /// Create new part (POST)
        /// Creates InitialStock movement record if quantity > 0
        /// </summary>
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] Part model)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                _logger.LogInformation(
                    "Creating part: Name='{PartName}', Quantity={Quantity}, CompanyId={CompanyId}, UserId={UserId}",
                    model.PartName,
                    model.Quantity,
                    companyId,
                    userId);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Part creation failed: User not authenticated");
                    return Unauthorized(new { success = false, message = "User not authenticated." });
                }

                // Validation
                if (string.IsNullOrWhiteSpace(model.PartName))
                {
                    return BadRequest(new { success = false, message = "Part name is required." });
                }

                if (model.UnitCost.HasValue && model.UnitCost.Value < 0)
                {
                    return BadRequest(new { success = false, message = "Unit cost cannot be negative." });
                }

                if (model.Quantity < 0)
                {
                    return BadRequest(new { success = false, message = "Quantity cannot be negative." });
                }

                // Check for duplicate part number within company
                if (!string.IsNullOrWhiteSpace(model.PartNumber))
                {
                    var duplicate = await _context.Parts
                        .AnyAsync(p => p.CompanyId == companyId && 
                                      p.PartNumber == model.PartNumber);

                    if (duplicate)
                    {
                        return BadRequest(new { success = false, message = "Part number already exists in your inventory." });
                    }
                }

                // Use execution strategy for transaction with retry support
                var strategy = _context.Database.CreateExecutionStrategy();
                
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // Create the part
                        var part = new Part
                        {
                            CompanyId = companyId,
                            PartName = model.PartName.Trim(),
                            PartNumber = model.PartNumber?.Trim(),
                            Description = model.Description?.Trim(),
                            Quantity = model.Quantity,
                            UnitCost = model.UnitCost,
                            ReorderLevel = model.ReorderLevel,
                            Location = model.Location?.Trim(),
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };

                        _context.Parts.Add(part);
                        await _context.SaveChangesAsync(); // Save to get PartId

                        _logger.LogInformation(
                            "Part created successfully: PartId={PartId}, Name='{PartName}'",
                            part.PartId,
                            part.PartName);

                        // Create InitialStock movement record if quantity > 0
                        if (part.Quantity > 0)
                        {
                            _logger.LogInformation(
                                "Creating InitialStock movement for Part #{PartId}, Quantity={Quantity}",
                                part.PartId,
                                part.Quantity);

                            var movement = new InventoryMovement
                            {
                                CompanyId = companyId,
                                PartId = part.PartId,
                                QuantityChanged = part.Quantity,
                                PreviousQuantity = 0,
                                NewQuantity = part.Quantity,
                                MovementType = InventoryMovementTypes.InitialStock,
                                WorkOrderId = null,
                                WorkOrderPartId = null,
                                PerformedByUserId = userId,
                                UnitCostSnapshot = part.UnitCost,
                                TotalCost = part.Quantity * (part.UnitCost ?? 0),
                                Notes = $"Initial stock for new part: {part.PartName}"
                                // CreatedAt will use database default (GETUTCDATE())
                            };

                            _context.InventoryMovements.Add(movement);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation(
                                "InitialStock movement created: MovementId={MovementId}, Part #{PartId}, Quantity={Quantity}",
                                movement.MovementId,
                                part.PartId,
                                part.Quantity);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "No InitialStock movement created for Part #{PartId} (quantity is zero)",
                                part.PartId);
                        }

                        // Commit transaction
                        await transaction.CommitAsync();

                        _logger.LogInformation(
                            "Part creation transaction committed successfully: PartId={PartId}",
                            part.PartId);
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                // Get the created part ID (it's available after the strategy executes)
                var createdPart = await _context.Parts
                    .Where(p => p.CompanyId == companyId && p.PartName == model.PartName.Trim())
                    .OrderByDescending(p => p.PartId)
                    .FirstOrDefaultAsync();

                return Ok(new { 
                    success = true, 
                    message = "Part created successfully!",
                    partId = createdPart?.PartId ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Unexpected error creating part '{PartName}': {ErrorMessage}",
                    model?.PartName ?? "Unknown",
                    ex.Message);
                
                return StatusCode(500, new { 
                    success = false, 
                    message = "An unexpected error occurred.",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Update existing part (PUT)
        /// </summary>
        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Part model)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var part = await _context.Parts
                .FirstOrDefaultAsync(p => p.PartId == id && p.CompanyId == companyId);

            if (part == null)
            {
                return NotFound(new { success = false, message = "Part not found." });
            }

            // Validation
            if (string.IsNullOrWhiteSpace(model.PartName))
            {
                return BadRequest(new { success = false, message = "Part name is required." });
            }

            if (model.UnitCost.HasValue && model.UnitCost.Value < 0)
            {
                return BadRequest(new { success = false, message = "Unit cost cannot be negative." });
            }

            if (model.Quantity < 0)
            {
                return BadRequest(new { success = false, message = "Quantity cannot be negative." });
            }

            // Check for duplicate part number (excluding current part)
            if (!string.IsNullOrWhiteSpace(model.PartNumber))
            {
                var duplicate = await _context.Parts
                    .AnyAsync(p => p.CompanyId == companyId && 
                                  p.PartNumber == model.PartNumber &&
                                  p.PartId != id);

                if (duplicate)
                {
                    return BadRequest(new { success = false, message = "Part number already exists in your inventory." });
                }
            }

            // Update fields
            part.PartName = model.PartName.Trim();
            part.PartNumber = model.PartNumber?.Trim();
            part.Description = model.Description?.Trim();
            part.Quantity = model.Quantity;
            part.UnitCost = model.UnitCost;
            part.ReorderLevel = model.ReorderLevel;
            part.Location = model.Location?.Trim();
            part.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = "Part updated successfully!"
            });
        }

        /// <summary>
        /// Toggle part active status
        /// </summary>
        [HttpPost]
        [Route("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var part = await _context.Parts
                .FirstOrDefaultAsync(p => p.PartId == id && p.CompanyId == companyId);

            if (part == null)
            {
                return NotFound(new { success = false, message = "Part not found." });
            }

            part.IsActive = !part.IsActive;
            part.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = $"Part {(part.IsActive ? "activated" : "deactivated")} successfully!",
                isActive = part.IsActive
            });
        }

        /// <summary>
        /// Adjust part quantity (for manual inventory adjustments)
        /// Creates immutable InventoryMovement record for audit trail
        /// </summary>
        [HttpPost]
        [Route("{id}/adjust-quantity")]
        public async Task<IActionResult> AdjustQuantity(int id, [FromBody] QuantityAdjustmentModel model)
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            try
            {
                // Use execution strategy for transaction with retry support
                var strategy = _context.Database.CreateExecutionStrategy();
                
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        var part = await _context.Parts
                            .FirstOrDefaultAsync(p => p.PartId == id && p.CompanyId == companyId);

                        if (part == null)
                        {
                            throw new InvalidOperationException("Part not found.");
                        }

                        var previousQuantity = part.Quantity;
                        var newQuantity = previousQuantity + model.AdjustmentAmount;

                        // NEGATIVE STOCK PREVENTION
                        if (newQuantity < 0)
                        {
                            throw new InvalidOperationException(
                                $"Adjustment would result in negative quantity. Current: {previousQuantity}, Adjustment: {model.AdjustmentAmount}");
                        }

                        // Zero adjustment check
                        if (model.AdjustmentAmount == 0)
                        {
                            throw new InvalidOperationException("No adjustment needed (amount is zero).");
                        }

                        // Update inventory
                        part.Quantity = newQuantity;
                        part.UpdatedAt = DateTime.Now;

                        // Determine movement type based on adjustment
                        string movementType;
                        if (model.AdjustmentAmount > 0)
                        {
                            // Positive adjustment - could be restock or correction
                            movementType = string.IsNullOrWhiteSpace(model.Reason) || 
                                           model.Reason.ToLower().Contains("restock") || 
                                           model.Reason.ToLower().Contains("received")
                                ? InventoryMovementTypes.Restock
                                : InventoryMovementTypes.Adjustment;
                        }
                        else
                        {
                            // Negative adjustment - could be correction or adjustment
                            movementType = string.IsNullOrWhiteSpace(model.Reason) || 
                                           model.Reason.ToLower().Contains("correction") || 
                                           model.Reason.ToLower().Contains("error")
                                ? InventoryMovementTypes.Correction
                                : InventoryMovementTypes.Adjustment;
                        }

                        // Create immutable inventory movement record
                        var movement = new InventoryMovement
                        {
                            CompanyId = companyId,
                            PartId = part.PartId,
                            QuantityChanged = model.AdjustmentAmount,
                            PreviousQuantity = previousQuantity,
                            NewQuantity = newQuantity,
                            MovementType = movementType,
                            WorkOrderId = null, // Manual adjustment, not WO-related
                            WorkOrderPartId = null,
                            PerformedByUserId = userId,
                            UnitCostSnapshot = part.UnitCost,
                            TotalCost = Math.Abs(model.AdjustmentAmount) * (part.UnitCost ?? 0),
                            Notes = string.IsNullOrWhiteSpace(model.Reason) 
                                ? $"Manual {movementType.ToLower()}: {(model.AdjustmentAmount > 0 ? "+" : "")}{model.AdjustmentAmount} units"
                                : model.Reason.Trim()
                        };

                        _context.InventoryMovements.Add(movement);

                        // Commit all changes atomically
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation(
                            "Manual inventory adjustment: Part #{PartId} '{PartName}', Amount: {Amount}, Stock: {Previous} → {New}, User: {UserId}",
                            part.PartId,
                            part.PartName,
                            model.AdjustmentAmount,
                            previousQuantity,
                            newQuantity,
                            userId);
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                // If we get here, the transaction succeeded
                var updatedPart = await _context.Parts
                    .FirstOrDefaultAsync(p => p.PartId == id && p.CompanyId == companyId);

                return Ok(new { 
                    success = true, 
                    message = "Quantity adjusted successfully!",
                    newQuantity = updatedPart?.Quantity ?? 0,
                    adjustmentAmount = model.AdjustmentAmount
                });
            }
            catch (InvalidOperationException ex)
            {
                // Business logic errors
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error adjusting quantity for Part #{PartId}: {ErrorMessage}",
                    id,
                    ex.Message);
                
                return StatusCode(500, new { 
                    success = false, 
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get inventory movement history for a specific part
        /// Shows complete audit trail of all stock changes
        /// </summary>
        [HttpGet]
        [Route("{id}/movement-history")]
        public async Task<IActionResult> GetMovementHistory(int id, int limit = 50)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Verify part exists and belongs to company
            var partExists = await _context.Parts
                .AnyAsync(p => p.PartId == id && p.CompanyId == companyId);

            if (!partExists)
            {
                return NotFound(new { success = false, message = "Part not found." });
            }

            // Get movement history
            var movements = await _context.InventoryMovements
                .Where(im => im.PartId == id && im.CompanyId == companyId)
                .Include(im => im.PerformedByUser)
                .Include(im => im.WorkOrder)
                .OrderByDescending(im => im.CreatedAt)
                .Take(limit)
                .Select(im => new
                {
                    movementId = im.MovementId,
                    movementType = im.MovementType,
                    quantityChanged = im.QuantityChanged,
                    previousQuantity = im.PreviousQuantity,
                    newQuantity = im.NewQuantity,
                    unitCostSnapshot = im.UnitCostSnapshot,
                    totalCost = im.TotalCost,
                    workOrderId = im.WorkOrderId,
                    workOrderNumber = im.WorkOrder != null ? $"WO-{im.WorkOrder.WorkOrderId}" : null,
                    performedBy = im.PerformedByUser != null ? im.PerformedByUser.Email : "System",
                    notes = im.Notes,
                    createdAt = im.CreatedAt
                })
                .ToListAsync();

            return Ok(new { 
                success = true, 
                movements = movements,
                totalRecords = movements.Count
            });
        }
    }

    /// <summary>
    /// Model for quantity adjustment requests
    /// </summary>
    public class QuantityAdjustmentModel
    {
        public int AdjustmentAmount { get; set; } // Can be positive or negative
        public string? Reason { get; set; }
    }
}
