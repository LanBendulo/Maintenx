using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Data;
using IT15_Project.Services;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// Cost Tracking Controller
    /// Provides visibility into maintenance costs across work orders, assets, and parts
    /// Integrates with existing cost data from WorkOrderCost, InventoryMovement, and MaintenanceLog
    /// MULTI-TENANT: All queries filtered by CompanyId
    /// </summary>
    [Authorize(Roles = "Owner,Admin,Supervisor")]
    [Route("admin/cost-tracking")]
    public class CostTrackingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public CostTrackingController(
            ApplicationDbContext context,
            ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        /// <summary>
        /// Cost Tracking Dashboard - Overview of all maintenance costs
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index(
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? assetId = null)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Default to current month if no dates provided
            if (!startDate.HasValue)
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!endDate.HasValue)
            {
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
            }

            // Get work order costs within date range
            var workOrderCostsQuery = _context.WorkOrderCosts
                .Include(woc => woc.WorkOrder)
                    .ThenInclude(wo => wo!.Asset)
                .Where(woc => woc.CompanyId == companyId &&
                             woc.CreatedAt >= startDate!.Value &&
                             woc.CreatedAt <= endDate!.Value);

            if (assetId.HasValue)
            {
                workOrderCostsQuery = workOrderCostsQuery.Where(woc => woc.WorkOrder != null && woc.WorkOrder.AssetId == assetId.Value);
            }

            var workOrderCosts = await workOrderCostsQuery.ToListAsync();

            // Calculate summary metrics
            var totalLaborCost = workOrderCosts.Sum(woc => woc.LaborCost ?? 0);
            var totalPartsCost = workOrderCosts.Sum(woc => woc.PartsCost ?? 0);
            var totalOtherCost = workOrderCosts.Sum(woc => woc.OtherCost ?? 0);
            var totalCost = workOrderCosts.Sum(woc => woc.TotalCost ?? 0);

            // Get top 5 most expensive assets
            var topAssetCosts = workOrderCosts
                .Where(woc => woc.WorkOrder?.Asset != null)
                .GroupBy(woc => new { woc.WorkOrder!.AssetId, woc.WorkOrder.Asset!.AssetName })
                .Select(g => new
                {
                    AssetId = g.Key.AssetId,
                    AssetName = g.Key.AssetName,
                    TotalCost = g.Sum(woc => woc.TotalCost ?? 0),
                    WorkOrderCount = g.Count()
                })
                .OrderByDescending(a => a.TotalCost)
                .Take(5)
                .ToList();

            // Get monthly trend (last 6 months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var monthlyTrend = await _context.WorkOrderCosts
                .Where(woc => woc.CompanyId == companyId && woc.CreatedAt >= sixMonthsAgo)
                .GroupBy(woc => new { woc.CreatedAt.Year, woc.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalCost = g.Sum(woc => woc.TotalCost ?? 0)
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            // Get assets for filter dropdown
            var assets = await _context.Assets
                .Where(a => a.CompanyId == companyId)
                .OrderBy(a => a.AssetName)
                .Select(a => new { a.AssetId, a.AssetName })
                .ToListAsync();

            // Pass data to view
            ViewBag.TotalLaborCost = totalLaborCost;
            ViewBag.TotalPartsCost = totalPartsCost;
            ViewBag.TotalOtherCost = totalOtherCost;
            ViewBag.TotalCost = totalCost;
            ViewBag.WorkOrderCount = workOrderCosts.Count;
            ViewBag.TopAssetCosts = topAssetCosts;
            ViewBag.MonthlyTrend = monthlyTrend;
            ViewBag.Assets = assets;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.AssetFilter = assetId;

            ViewData["Active"] = "CostTracking";
            return View(workOrderCosts);
        }

        /// <summary>
        /// Get cost breakdown by asset (JSON)
        /// </summary>
        [HttpGet]
        [Route("asset/{assetId}")]
        public async Task<IActionResult> GetAssetCostBreakdown(int assetId)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Verify asset exists and belongs to company
            var assetExists = await _context.Assets
                .AnyAsync(a => a.AssetId == assetId && a.CompanyId == companyId);

            if (!assetExists)
            {
                return NotFound(new { success = false, message = "Asset not found." });
            }

            // Get all work order costs for this asset
            var costs = await _context.WorkOrderCosts
                .Include(woc => woc.WorkOrder)
                .Where(woc => woc.CompanyId == companyId && woc.WorkOrder != null && woc.WorkOrder.AssetId == assetId)
                .OrderByDescending(woc => woc.CreatedAt)
                .Select(woc => new
                {
                    workOrderId = woc.WorkOrderId,
                    workOrderNumber = $"WO-{woc.WorkOrderId:D4}",
                    laborCost = woc.LaborCost,
                    partsCost = woc.PartsCost,
                    otherCost = woc.OtherCost,
                    totalCost = woc.TotalCost,
                    createdAt = woc.CreatedAt,
                    status = woc.WorkOrder!.Status
                })
                .ToListAsync();

            var totalCost = costs.Sum(c => c.totalCost ?? 0);
            var totalLabor = costs.Sum(c => c.laborCost ?? 0);
            var totalParts = costs.Sum(c => c.partsCost ?? 0);
            var totalOther = costs.Sum(c => c.otherCost ?? 0);

            return Ok(new
            {
                success = true,
                costs = costs,
                summary = new
                {
                    totalCost = totalCost,
                    totalLabor = totalLabor,
                    totalParts = totalParts,
                    totalOther = totalOther,
                    workOrderCount = costs.Count
                }
            });
        }

        /// <summary>
        /// Get cost summary by date range (JSON)
        /// </summary>
        [HttpGet]
        [Route("summary")]
        public async Task<IActionResult> GetCostSummary(DateTime? startDate = null, DateTime? endDate = null)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Default to current month
            if (!startDate.HasValue)
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!endDate.HasValue)
            {
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
            }

            var costs = await _context.WorkOrderCosts
                .Where(woc => woc.CompanyId == companyId &&
                             woc.CreatedAt >= startDate.Value &&
                             woc.CreatedAt <= endDate.Value)
                .ToListAsync();

            var summary = new
            {
                totalCost = costs.Sum(c => c.TotalCost ?? 0),
                laborCost = costs.Sum(c => c.LaborCost ?? 0),
                partsCost = costs.Sum(c => c.PartsCost ?? 0),
                otherCost = costs.Sum(c => c.OtherCost ?? 0),
                workOrderCount = costs.Count,
                averageCostPerWorkOrder = costs.Any() ? costs.Average(c => c.TotalCost ?? 0) : 0
            };

            return Ok(new { success = true, summary = summary });
        }

        /// <summary>
        /// Export cost data to CSV (future enhancement)
        /// </summary>
        [HttpGet]
        [Route("export")]
        public IActionResult Export(DateTime? startDate = null, DateTime? endDate = null)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // TODO: Implement CSV export
            return Ok(new { success = true, message = "Export feature coming soon" });
        }
    }
}
