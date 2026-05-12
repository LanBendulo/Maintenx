using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IT15_Project.Services.Archiving
{
    /// <summary>
    /// Enterprise-grade soft archival service for Work Orders
    /// Implements lifecycle-governed operational retention
    /// Preserves historical records, PM traceability, and audit trails
    /// </summary>
    public class ArchiveService : IArchiveService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ArchiveService> _logger;

        // Statuses that allow archival (operationally complete)
        private readonly HashSet<string> _archivableStatuses = new()
        {
            WorkOrderStatuses.Completed,
            WorkOrderStatuses.Cancelled
        };

        public ArchiveService(
            ApplicationDbContext context,
            ILogger<ArchiveService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Archives a work order with full lifecycle validation
        /// </summary>
        public async Task<(bool Success, string Message)> ArchiveWorkOrderAsync(
            int workOrderId, 
            string userId, 
            string reason)
        {
            try
            {
                // Validate work order exists
                var workOrder = await _context.WorkOrders
                    .Include(wo => wo.Company)
                    .Include(wo => wo.MaintenanceRequest)
                    .FirstOrDefaultAsync(wo => wo.WorkOrderId == workOrderId);

                if (workOrder == null)
                {
                    return (false, "Work order not found.");
                }

                // Check if already archived
                if (workOrder.IsArchived)
                {
                    return (false, "Work order is already archived.");
                }

                // Validate lifecycle state
                var canArchive = await CanArchiveWorkOrderAsync(workOrderId);
                if (!canArchive.CanArchive)
                {
                    return (false, canArchive.Message);
                }

                // Validate reason provided
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return (false, "Archive reason is required.");
                }

                // Perform soft archive
                workOrder.IsArchived = true;
                workOrder.ArchivedAt = DateTime.UtcNow;
                workOrder.ArchivedByUserId = userId;
                workOrder.ArchiveReason = reason.Trim();

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Work Order archived: WO#{WorkOrderId}, Company: {CompanyId}, User: {UserId}, Reason: {Reason}",
                    workOrderId, workOrder.CompanyId, userId, reason);

                return (true, "Work order archived successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error archiving work order {WorkOrderId} by user {UserId}", 
                    workOrderId, userId);
                return (false, "An error occurred while archiving the work order.");
            }
        }

        /// <summary>
        /// Restores an archived work order
        /// </summary>
        public async Task<(bool Success, string Message)> RestoreWorkOrderAsync(
            int workOrderId, 
            string userId)
        {
            try
            {
                var workOrder = await _context.WorkOrders
                    .FirstOrDefaultAsync(wo => wo.WorkOrderId == workOrderId);

                if (workOrder == null)
                {
                    return (false, "Work order not found.");
                }

                if (!workOrder.IsArchived)
                {
                    return (false, "Work order is not archived.");
                }

                // Restore from archive
                workOrder.IsArchived = false;
                workOrder.ArchivedAt = null;
                workOrder.ArchivedByUserId = null;
                workOrder.ArchiveReason = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Work Order restored: WO#{WorkOrderId}, Company: {CompanyId}, User: {UserId}",
                    workOrderId, workOrder.CompanyId, userId);

                return (true, "Work order restored successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error restoring work order {WorkOrderId} by user {UserId}", 
                    workOrderId, userId);
                return (false, "An error occurred while restoring the work order.");
            }
        }

        /// <summary>
        /// Validates if a work order can be archived based on lifecycle rules
        /// </summary>
        public async Task<(bool CanArchive, string Message)> CanArchiveWorkOrderAsync(int workOrderId)
        {
            var workOrder = await _context.WorkOrders
                .Include(wo => wo.MaintenanceRequest)
                .FirstOrDefaultAsync(wo => wo.WorkOrderId == workOrderId);

            if (workOrder == null)
            {
                return (false, "Work order not found.");
            }

            // Rule 1: Check lifecycle status
            if (!_archivableStatuses.Contains(workOrder.Status ?? ""))
            {
                return (false, 
                    $"Only work orders with status 'Completed' or 'Cancelled' can be archived. " +
                    $"Current status: {workOrder.Status}");
            }

            // Rule 2: Check if linked Maintenance Request is also complete
            if (workOrder.MaintenanceRequestId.HasValue && workOrder.MaintenanceRequest != null)
            {
                var mrStatus = workOrder.MaintenanceRequest.Status;
                if (mrStatus != MaintenanceRequestStatuses.Closed && 
                    mrStatus != MaintenanceRequestStatuses.Converted &&
                    mrStatus != MaintenanceRequestStatuses.Rejected)
                {
                    return (false, 
                        "Cannot archive work order while linked Maintenance Request is still active.");
                }
            }

            // Rule 3: Check for active inventory operations (future enhancement)
            // This would check WorkOrder_Parts or inventory transactions
            // For now, we'll allow archival if status is complete

            return (true, "Work order can be archived.");
        }

        /// <summary>
        /// Gets all archived work orders for a company
        /// </summary>
        public async Task<List<WorkOrder>> GetArchivedWorkOrdersAsync(int companyId)
        {
            return await _context.WorkOrders
                .Include(wo => wo.Asset)
                .Include(wo => wo.AssignedToPersonnel)
                .Include(wo => wo.ArchivedByUser)
                .Where(wo => wo.CompanyId == companyId && wo.IsArchived)
                .OrderByDescending(wo => wo.ArchivedAt)
                .ToListAsync();
        }
    }
}
