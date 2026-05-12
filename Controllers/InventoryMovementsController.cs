using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Services;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// Inventory Movement Visibility Controller
    /// Provides audit and traceability UI for inventory mutations
    /// READ-ONLY: Does not modify inventory, only displays movement history
    /// MULTI-TENANT: All queries filtered by CompanyId
    /// </summary>
    [Authorize(Roles = "Owner,Admin,Supervisor")]
    [Route("admin/inventory-movements")]
    public class InventoryMovementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public InventoryMovementsController(
            ApplicationDbContext context,
            ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        /// <summary>
        /// Inventory Movement Logs - Main audit page
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index(
            string search = "",
            string movementType = "",
            string partFilter = "",
            int? workOrderId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Base query with tenant filtering
            var query = _context.InventoryMovements
                .Include(im => im.Part)
                .Include(im => im.WorkOrder)
                .Include(im => im.PerformedByUser)
                .Where(im => im.CompanyId == companyId)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(im =>
                    (im.Part != null && im.Part.PartName.ToLower().Contains(search)) ||
                    (im.Part != null && im.Part.PartNumber != null && im.Part.PartNumber.ToLower().Contains(search)) ||
                    (im.Notes != null && im.Notes.ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(movementType))
            {
                query = query.Where(im => im.MovementType == movementType);
            }

            if (!string.IsNullOrWhiteSpace(partFilter))
            {
                if (int.TryParse(partFilter, out int partId))
                {
                    query = query.Where(im => im.PartId == partId);
                }
            }

            if (workOrderId.HasValue)
            {
                query = query.Where(im => im.WorkOrderId == workOrderId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(im => im.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(im => im.CreatedAt <= endOfDay);
            }

            // Get movements ordered by most recent first
            var movements = await query
                .OrderByDescending(im => im.CreatedAt)
                .Take(500) // Limit for performance
                .ToListAsync();

            // Calculate summary metrics
            var allMovements = await _context.InventoryMovements
                .Where(im => im.CompanyId == companyId)
                .ToListAsync();

            ViewBag.TotalMovements = allMovements.Count;
            ViewBag.ConsumptionCount = allMovements.Count(im => im.MovementType == InventoryMovementTypes.Consumption);
            ViewBag.RestockCount = allMovements.Count(im => im.MovementType == InventoryMovementTypes.Restock);
            ViewBag.AdjustmentCount = allMovements.Count(im => 
                im.MovementType == InventoryMovementTypes.Adjustment || 
                im.MovementType == InventoryMovementTypes.Correction);

            // Get parts list for filter dropdown
            var parts = await _context.Parts
                .Where(p => p.CompanyId == companyId && p.IsActive)
                .OrderBy(p => p.PartName)
                .Select(p => new { p.PartId, p.PartName, p.PartNumber })
                .ToListAsync();

            ViewBag.Parts = parts;
            ViewBag.SearchFilter = search;
            ViewBag.MovementTypeFilter = movementType;
            ViewBag.PartFilter = partFilter;
            ViewBag.WorkOrderFilter = workOrderId;
            ViewBag.StartDateFilter = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDateFilter = endDate?.ToString("yyyy-MM-dd");

            ViewData["Active"] = "InventoryMovements";
            return View(movements);
        }

        /// <summary>
        /// Get movement history for a specific part (JSON)
        /// Used in Part Details page
        /// </summary>
        [HttpGet]
        [Route("part/{partId}")]
        public async Task<IActionResult> GetPartMovementHistory(int partId, int limit = 50)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Verify part exists and belongs to company
            var partExists = await _context.Parts
                .AnyAsync(p => p.PartId == partId && p.CompanyId == companyId);

            if (!partExists)
            {
                return NotFound(new { success = false, message = "Part not found." });
            }

            // Get movement history
            var movements = await _context.InventoryMovements
                .Where(im => im.PartId == partId && im.CompanyId == companyId)
                .Include(im => im.WorkOrder)
                .Include(im => im.PerformedByUser)
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
                    workOrderNumber = im.WorkOrder != null ? $"WO-{im.WorkOrder.WorkOrderId:D4}" : null,
                    performedBy = im.PerformedByUser != null ? im.PerformedByUser.Email : "System",
                    notes = im.Notes,
                    createdAt = im.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                movements = movements,
                totalRecords = movements.Count
            });
        }

        /// <summary>
        /// Get parts consumption for a specific work order (JSON)
        /// Used in Work Order Details page
        /// </summary>
        [HttpGet]
        [Route("work-order/{workOrderId}")]
        public async Task<IActionResult> GetWorkOrderConsumption(int workOrderId)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Verify work order exists and belongs to company
            var workOrderExists = await _context.WorkOrders
                .AnyAsync(w => w.WorkOrderId == workOrderId && w.CompanyId == companyId);

            if (!workOrderExists)
            {
                return NotFound(new { success = false, message = "Work order not found." });
            }

            // Get consumed parts with movement details
            var consumedParts = await _context.WorkOrderParts
                .Where(wop => wop.WorkOrderId == workOrderId && 
                             wop.CompanyId == companyId &&
                             wop.UsageStatus == PartUsageStatuses.Consumed)
                .Include(wop => wop.Part)
                .Include(wop => wop.AddedByPersonnel)
                .Select(wop => new
                {
                    partId = wop.PartId,
                    partName = wop.Part != null ? wop.Part.PartName : "Unknown",
                    partNumber = wop.Part != null ? wop.Part.PartNumber : null,
                    quantityUsed = wop.QuantityUsed,
                    unitCost = wop.UnitCost,
                    totalCost = wop.TotalCost,
                    usageStatus = wop.UsageStatus,
                    consumedAt = wop.ConsumedAt,
                    addedBy = wop.AddedByPersonnel != null ? wop.AddedByPersonnel.FullName : "N/A"
                })
                .ToListAsync();

            // Get inventory movements for this work order
            var movements = await _context.InventoryMovements
                .Where(im => im.WorkOrderId == workOrderId && im.CompanyId == companyId)
                .Include(im => im.Part)
                .OrderBy(im => im.CreatedAt)
                .Select(im => new
                {
                    movementId = im.MovementId,
                    partName = im.Part != null ? im.Part.PartName : "Unknown",
                    movementType = im.MovementType,
                    quantityChanged = im.QuantityChanged,
                    previousQuantity = im.PreviousQuantity,
                    newQuantity = im.NewQuantity,
                    unitCostSnapshot = im.UnitCostSnapshot,
                    totalCost = im.TotalCost,
                    createdAt = im.CreatedAt
                })
                .ToListAsync();

            // Calculate total material cost
            var totalMaterialCost = consumedParts.Sum(p => p.totalCost ?? 0);

            return Ok(new
            {
                success = true,
                consumedParts = consumedParts,
                movements = movements,
                totalMaterialCost = totalMaterialCost,
                partsCount = consumedParts.Count
            });
        }

        /// <summary>
        /// Export movements to CSV (future enhancement)
        /// </summary>
        [HttpGet]
        [Route("export")]
        public async Task<IActionResult> Export()
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var movements = await _context.InventoryMovements
                .Include(im => im.Part)
                .Include(im => im.WorkOrder)
                .Include(im => im.PerformedByUser)
                .Where(im => im.CompanyId == companyId)
                .OrderByDescending(im => im.CreatedAt)
                .ToListAsync();

            // TODO: Implement CSV export
            return Ok(new { success = true, message = "Export feature coming soon" });
        }
    }
}
