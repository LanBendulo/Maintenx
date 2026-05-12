using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Data;
using IT15_Project.Models;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// Supervisor Dashboard Controller
    /// Provides operational oversight and approval workflows
    /// </summary>
    [Authorize(Roles = "Supervisor")]
    public class SupervisorDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SupervisorDashboardController> _logger;

        public SupervisorDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SupervisorDashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// GET: /supervisor/dashboard
        /// Main supervisor dashboard with oversight metrics
        /// </summary>
        [HttpGet("/supervisor/dashboard")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Unauthorized();
            }

            var companyId = currentUser.CompanyId.Value;

            // Get dashboard metrics
            var activeWorkOrders = await _context.WorkOrders
                .Where(w => w.CompanyId == companyId && 
                           (w.Status == "Pending" || w.Status == "In Progress"))
                .CountAsync();

            var pendingApprovals = await _context.WorkOrderParts
                .Where(wp => wp.CompanyId == companyId && wp.UsageStatus == "Pending")
                .CountAsync();

            var technicians = await _context.Personnel
                .Where(p => p.CompanyId == companyId && p.Role == "Technician" && p.IsActive)
                .CountAsync();

            var completedToday = await _context.WorkOrders
                .Where(w => w.CompanyId == companyId && 
                           w.Status == "Completed" && 
                           w.ActualCompletion.HasValue &&
                           w.ActualCompletion.Value.Date == DateTime.Today)
                .CountAsync();

            ViewBag.ActiveWorkOrders = activeWorkOrders;
            ViewBag.PendingApprovals = pendingApprovals;
            ViewBag.Technicians = technicians;
            ViewBag.CompletedToday = completedToday;

            return View();
        }

        /// <summary>
        /// GET: /supervisor/work-orders
        /// View all work orders for oversight
        /// </summary>
        [HttpGet("/supervisor/work-orders")]
        public async Task<IActionResult> WorkOrders()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Unauthorized();
            }

            var companyId = currentUser.CompanyId.Value;

            var workOrders = await _context.WorkOrders
                .Where(w => w.CompanyId == companyId)
                .Include(w => w.Asset)
                .Include(w => w.AssignedToPersonnel)
                .OrderByDescending(w => w.DateCreated)
                .ToListAsync();

            return View(workOrders);
        }

        /// <summary>
        /// GET: /supervisor/pending-approvals
        /// View pending parts approvals
        /// </summary>
        [HttpGet("/supervisor/pending-approvals")]
        public async Task<IActionResult> PendingApprovals()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Unauthorized();
            }

            var companyId = currentUser.CompanyId.Value;

            var pendingParts = await _context.WorkOrderParts
                .Where(wp => wp.CompanyId == companyId && wp.UsageStatus == "Pending")
                .Include(wp => wp.WorkOrder)
                    .ThenInclude(wo => wo.Asset)
                .Include(wp => wp.Part)
                .Include(wp => wp.AddedByPersonnel)
                .OrderBy(wp => wp.CreatedAt)
                .ToListAsync();

            return View(pendingParts);
        }

        /// <summary>
        /// GET: /supervisor/technician-oversight
        /// View technician workload and performance
        /// </summary>
        [HttpGet("/supervisor/technician-oversight")]
        public async Task<IActionResult> TechnicianOversight()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Unauthorized();
            }

            var companyId = currentUser.CompanyId.Value;

            var technicians = await _context.Personnel
                .Where(p => p.CompanyId == companyId && p.Role == "Technician" && p.IsActive)
                .Select(p => new
                {
                    Personnel = p,
                    ActiveWorkOrders = _context.WorkOrders
                        .Count(w => w.CompanyId == companyId && 
                                   w.AssignedTo == p.PersonnelId && 
                                   (w.Status == "Pending" || w.Status == "In Progress")),
                    CompletedThisMonth = _context.WorkOrders
                        .Count(w => w.CompanyId == companyId && 
                                   w.AssignedTo == p.PersonnelId && 
                                   w.Status == "Completed" &&
                                   w.ActualCompletion.HasValue &&
                                   w.ActualCompletion.Value.Month == DateTime.Now.Month &&
                                   w.ActualCompletion.Value.Year == DateTime.Now.Year)
                })
                .ToListAsync();

            return View(technicians);
        }

        /// <summary>
        /// POST: /supervisor/approve-part/{id}
        /// Approve staged parts usage
        /// </summary>
        [HttpPost("/supervisor/approve-part/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePart(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var companyId = currentUser.CompanyId.Value;

            var workOrderPart = await _context.WorkOrderParts
                .Include(wp => wp.Part)
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.CompanyId == companyId);

            if (workOrderPart == null)
            {
                return Json(new { success = false, message = "Part usage not found" });
            }

            if (workOrderPart.UsageStatus != "Pending")
            {
                return Json(new { success = false, message = "Part usage is not pending approval" });
            }

            // Check stock availability
            if (workOrderPart.Part!.Quantity < workOrderPart.QuantityUsed)
            {
                return Json(new { success = false, message = "Insufficient stock available" });
            }

            try
            {
                // Use execution strategy for transaction
                await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Update part usage status
                        workOrderPart.UsageStatus = "Approved";
                        workOrderPart.ApprovedByUserId = currentUser.Id;
                        workOrderPart.UpdatedAt = DateTime.UtcNow;

                        // Deduct inventory
                        workOrderPart.Part.Quantity -= workOrderPart.QuantityUsed;
                        workOrderPart.Part.UpdatedAt = DateTime.UtcNow;

                        // Create inventory movement record
                        var movement = new InventoryMovement
                        {
                            CompanyId = companyId,
                            PartId = workOrderPart.PartId,
                            WorkOrderId = workOrderPart.WorkOrderId,
                            MovementType = "Consumption",
                            QuantityChanged = -workOrderPart.QuantityUsed,
                            PreviousQuantity = workOrderPart.Part.Quantity + workOrderPart.QuantityUsed,
                            NewQuantity = workOrderPart.Part.Quantity,
                            UnitCostSnapshot = workOrderPart.UnitCost,
                            TotalCost = workOrderPart.TotalCost,
                            PerformedByUserId = currentUser.Id,
                            Notes = $"Approved by Supervisor: {currentUser.FullName}",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.InventoryMovements.Add(movement);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation(
                            "Supervisor {SupervisorId} approved part usage {PartUsageId} for WO {WorkOrderId}",
                            currentUser.Id, id, workOrderPart.WorkOrderId);
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                return Json(new { success = true, message = "Part usage approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving part usage {PartUsageId}", id);
                return Json(new { success = false, message = "Error approving part usage", error = ex.Message });
            }
        }

        /// <summary>
        /// POST: /supervisor/reject-part/{id}
        /// Reject staged parts usage
        /// </summary>
        [HttpPost("/supervisor/reject-part/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPart(int id, [FromForm] string? reason)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var companyId = currentUser.CompanyId.Value;

            var workOrderPart = await _context.WorkOrderParts
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.CompanyId == companyId);

            if (workOrderPart == null)
            {
                return Json(new { success = false, message = "Part usage not found" });
            }

            if (workOrderPart.UsageStatus != "Pending")
            {
                return Json(new { success = false, message = "Part usage is not pending approval" });
            }

            try
            {
                workOrderPart.UsageStatus = "Rejected";
                workOrderPart.ApprovedByUserId = currentUser.Id;
                workOrderPart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Supervisor {SupervisorId} rejected part usage {PartUsageId} for WO {WorkOrderId}. Reason: {Reason}",
                    currentUser.Id, id, workOrderPart.WorkOrderId, reason ?? "Not specified");

                return Json(new { success = true, message = "Part usage rejected" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting part usage {PartUsageId}", id);
                return Json(new { success = false, message = "Error rejecting part usage", error = ex.Message });
            }
        }

        /// <summary>
        /// GET: /supervisor/work-order/{id}
        /// View work order details
        /// </summary>
        [HttpGet("/supervisor/work-order/{id}")]
        public async Task<IActionResult> WorkOrderDetails(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Unauthorized();
            }

            var companyId = currentUser.CompanyId.Value;

            var workOrder = await _context.WorkOrders
                .Where(w => w.WorkOrderId == id && w.CompanyId == companyId)
                .Include(w => w.Asset)
                .Include(w => w.AssignedToPersonnel)
                .Include(w => w.MaintenanceRequest)
                .FirstOrDefaultAsync();

            if (workOrder == null)
            {
                return NotFound();
            }

            // Get parts used
            var parts = await _context.WorkOrderParts
                .Where(wp => wp.WorkOrderId == id && wp.CompanyId == companyId)
                .Include(wp => wp.Part)
                .Include(wp => wp.AddedByPersonnel)
                .Include(wp => wp.ApprovedByUser)
                .ToListAsync();

            ViewBag.Parts = parts;

            return View(workOrder);
        }
        /// <summary>
        /// GET: /supervisor/pending-approvals-count
        /// Get count of pending approvals for badge
        /// </summary>
        [HttpGet("/supervisor/pending-approvals-count")]
        public async Task<IActionResult> GetPendingApprovalsCount()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Json(new { count = 0 });
            }

            var companyId = currentUser.CompanyId.Value;

            var count = await _context.WorkOrderParts
                .Where(wp => wp.CompanyId == companyId && wp.UsageStatus == "Pending")
                .CountAsync();

            return Json(new { count });
        }

        /// <summary>
        /// GET: /supervisor/pm-monitoring
        /// Preventive Maintenance monitoring (read-only)
        /// </summary>
        [HttpGet("/supervisor/pm-monitoring")]
        public async Task<IActionResult> PmMonitoring(string filter = "active")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Unauthorized();
            }

            var companyId = currentUser.CompanyId.Value;

            var query = _context.PreventiveSchedules
                .Where(ps => ps.CompanyId == companyId)
                .Include(ps => ps.Asset)
                .Include(ps => ps.DefaultTechnician)
                .AsQueryable();

            // Filter by active status
            switch (filter.ToLower())
            {
                case "inactive":
                    query = query.Where(ps => !ps.IsActive);
                    break;
                case "all":
                    // No filter
                    break;
                case "active":
                default:
                    query = query.Where(ps => ps.IsActive);
                    break;
            }

            var schedules = await query
                .OrderBy(ps => ps.NextDueDate)
                .ToListAsync();

            ViewBag.Filter = filter;
            ViewData["Active"] = "PmMonitoring";
            return View(schedules);
        }

        /// <summary>
        /// GET: /supervisor/inventory-movements
        /// Inventory movement audit trail (read-only)
        /// </summary>
        [HttpGet("/supervisor/inventory-movements")]
        public async Task<IActionResult> InventoryMovements(
            string search = "",
            string movementType = "",
            string partFilter = "",
            int? workOrderId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Unauthorized();
            }

            var companyId = currentUser.CompanyId.Value;

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
            ViewBag.ConsumptionCount = allMovements.Count(im => im.MovementType == "Consumption");
            ViewBag.RestockCount = allMovements.Count(im => im.MovementType == "Restock");
            ViewBag.AdjustmentCount = allMovements.Count(im => 
                im.MovementType == "Adjustment" || 
                im.MovementType == "Correction");

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
        /// GET: /supervisor/maintenance-logs
        /// Maintenance log history (read-only)
        /// </summary>
        [HttpGet("/supervisor/maintenance-logs")]
        public async Task<IActionResult> MaintenanceLogs(string search = "")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Unauthorized();
            }

            var companyId = currentUser.CompanyId.Value;

            var query = _context.MaintenanceLogs
                .Where(ml => ml.CompanyId == companyId)
                .Include(ml => ml.Asset)
                .Include(ml => ml.CompletedByPersonnel)
                .Include(ml => ml.WorkOrder)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(ml => 
                    ml.Title.Contains(search) ||
                    ml.Asset!.AssetName.Contains(search) ||
                    ml.WorkOrderId.ToString().Contains(search));
            }

            var logs = await query
                .OrderByDescending(ml => ml.CompletedDate)
                .ToListAsync();

            ViewBag.Search = search;
            ViewData["Active"] = "MaintenanceLogs";
            return View(logs);
        }

        /// <summary>
        /// GET: /supervisor/cost-tracking
        /// Cost tracking overview (read-only)
        /// </summary>
        [HttpGet("/supervisor/cost-tracking")]
        public async Task<IActionResult> CostTracking(
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? assetId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Unauthorized();
            }

            var companyId = currentUser.CompanyId.Value;

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
        /// GET: /supervisor/maintenance-log/{id}
        /// Get maintenance log details (read-only)
        /// </summary>
        [HttpGet("/supervisor/maintenance-log/{id}")]
        public async Task<IActionResult> GetMaintenanceLogDetails(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.CompanyId == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var companyId = currentUser.CompanyId.Value;

            var log = await _context.MaintenanceLogs
                .Where(ml => ml.LogId == id && ml.CompanyId == companyId)
                .Include(ml => ml.Asset)
                .Include(ml => ml.CompletedByPersonnel)
                .Include(ml => ml.WorkOrder)
                .FirstOrDefaultAsync();

            if (log == null)
            {
                return Json(new { success = false, message = "Log not found" });
            }

            return Ok(new
            {
                logId = log.LogId,
                workOrderId = log.WorkOrderId,
                assetName = log.Asset?.AssetName,
                title = log.Title,
                description = log.Description,
                completedBy = log.CompletedByPersonnel?.FullName,
                completedDate = log.CompletedDate,
                notes = log.Notes,
                createdAt = log.CreatedAt
            });
        }
    }
}
