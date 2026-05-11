using IT15_Project.Constants;
using IT15_Project.Data;
using IT15_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_Project.Services
{
    /// <summary>
    /// Manages Asset operational status lifecycle based on Work Order state
    /// </summary>
    public class AssetStatusService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AssetStatusService> _logger;

        public AssetStatusService(
            ApplicationDbContext context,
            ILogger<AssetStatusService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Work Order statuses considered "active" (asset should be Under Maintenance)
        /// </summary>
        private static readonly string[] ActiveWorkOrderStatuses = WorkOrderStatuses.Active;

        /// <summary>
        /// Validates if a Work Order can be created for an asset
        /// Returns error message if validation fails, null if valid
        /// </summary>
        public async Task<string?> ValidateAssetForWorkOrderAsync(int assetId, int companyId)
        {
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.AssetId == assetId && a.CompanyId == companyId);

            if (asset == null)
                return "Asset not found.";

            // Check if asset is retired
            if (asset.Status == AssetStatuses.Retired)
                return "Cannot create work order for retired asset.";

            // Check for existing active work orders
            var hasActiveWorkOrder = await _context.WorkOrders
                .AnyAsync(wo => wo.AssetId == assetId &&
                               wo.CompanyId == companyId &&
                               (wo.Status == WorkOrderStatuses.Pending || wo.Status == WorkOrderStatuses.InProgress));

            if (hasActiveWorkOrder)
                return "This asset already has an active work order. Please complete or cancel the existing work order first.";

            return null; // Validation passed
        }

        /// <summary>
        /// Updates asset status when a Work Order is created
        /// </summary>
        public async Task OnWorkOrderCreatedAsync(int workOrderId, string? userId = null)
        {
            var workOrder = await _context.WorkOrders
                .Include(wo => wo.Asset)
                .FirstOrDefaultAsync(wo => wo.WorkOrderId == workOrderId);

            if (workOrder?.Asset == null)
            {
                _logger.LogWarning("Work Order {WorkOrderId} or its Asset not found", workOrderId);
                return;
            }

            await UpdateAssetStatusAsync(
                workOrder.Asset,
                AssetStatuses.UnderMaintenance,
                userId,
                workOrderId,
                $"Work Order #{workOrderId} created"
            );
        }

        /// <summary>
        /// Updates asset status when a Work Order is completed or cancelled
        /// </summary>
        public async Task OnWorkOrderCompletedOrCancelledAsync(int workOrderId, string? userId = null)
        {
            var workOrder = await _context.WorkOrders
                .Include(wo => wo.Asset)
                .FirstOrDefaultAsync(wo => wo.WorkOrderId == workOrderId);

            if (workOrder?.Asset == null)
            {
                _logger.LogWarning("Work Order {WorkOrderId} or its Asset not found", workOrderId);
                return;
            }

            // Check if there are other active work orders for this asset
            var hasOtherActiveWorkOrders = await _context.WorkOrders
                .AnyAsync(wo => wo.AssetId == workOrder.AssetId &&
                               wo.WorkOrderId != workOrderId &&
                               wo.CompanyId == workOrder.CompanyId &&
                               (wo.Status == WorkOrderStatuses.Pending || wo.Status == WorkOrderStatuses.InProgress));

            if (hasOtherActiveWorkOrders)
            {
                _logger.LogInformation(
                    "Asset {AssetId} remains Under Maintenance - other active work orders exist",
                    workOrder.AssetId
                );
                return; // Keep status as "Under Maintenance"
            }

            // No other active work orders - restore to Active
            await UpdateAssetStatusAsync(
                workOrder.Asset,
                AssetStatuses.Active,
                userId,
                workOrderId,
                $"Work Order #{workOrderId} {workOrder.Status?.ToLower()}"
            );
        }

        /// <summary>
        /// Core method to update asset status with audit trail
        /// </summary>
        private async Task UpdateAssetStatusAsync(
            Asset asset,
            string newStatus,
            string? userId,
            int? workOrderId,
            string reason)
        {
            var oldStatus = asset.Status;

            // Skip if status hasn't changed
            if (oldStatus == newStatus)
            {
                return;
            }

            // Update asset status
            asset.Status = newStatus;
            asset.UpdatedAt = DateTime.UtcNow;

            // Create audit history record
            var history = new AssetStatusHistory
            {
                AssetId = asset.AssetId,
                CompanyId = asset.CompanyId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedByUserId = userId,
                WorkOrderId = workOrderId,
                Reason = reason,
                ChangedAt = DateTime.UtcNow
            };

            _context.AssetStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Asset {AssetId} status changed: {OldStatus} → {NewStatus}. Reason: {Reason}",
                asset.AssetId,
                oldStatus ?? "null",
                newStatus,
                reason
            );
        }

        /// <summary>
        /// Gets asset status history for audit trail
        /// </summary>
        public async Task<List<AssetStatusHistory>> GetAssetStatusHistoryAsync(int assetId, int companyId)
        {
            return await _context.AssetStatusHistories
                .Where(h => h.AssetId == assetId && h.CompanyId == companyId)
                .OrderByDescending(h => h.ChangedAt)
                .Take(50)
                .ToListAsync();
        }

        /// <summary>
        /// Gets count of assets by status for dashboard metrics
        /// </summary>
        public async Task<Dictionary<string, int>> GetAssetStatusCountsAsync(int companyId)
        {
            var counts = await _context.Assets
                .Where(a => a.CompanyId == companyId)
                .GroupBy(a => a.Status ?? AssetStatuses.Active)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts.ToDictionary(x => x.Status, x => x.Count);
        }
    }
}
