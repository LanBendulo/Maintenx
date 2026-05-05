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
    /// Dashboard for Technician role only. Accessible at /dashboard.
    /// Shows ONLY technician-relevant data: assigned work orders and personal metrics.
    /// MULTI-TENANT: All queries filtered by CompanyId AND AssignedTo (technician ownership)
    /// </summary>
    [Authorize(Roles = "Technician")]
    [Route("dashboard")]
    public class TechnicianDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ICostService _costService;

        public TechnicianDashboardController(
            ApplicationDbContext context, 
            ITenantService tenantService,
            ICostService costService)
        {
            _context = context;
            _tenantService = tenantService;
            _costService = costService;
        }

        /// <summary>
        /// Technician Dashboard - Shows only assigned work orders and personal metrics
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get current technician's personnel record
            var personnel = await _context.Personnel
                .Where(p => p.CompanyId == companyId && p.UserId == userId)
                .FirstOrDefaultAsync();

            if (personnel == null)
            {
                return Unauthorized();
            }

            // Get assigned work orders (TECHNICIAN-SCOPED)
            var assignedWorkOrders = await _context.WorkOrders
                .Where(w => w.CompanyId == companyId && 
                           w.AssignedTo == personnel.PersonnelId &&
                           !w.IsArchived)
                .Include(w => w.Asset)
                .OrderByDescending(w => w.DateCreated)
                .ToListAsync();

            // Calculate metrics (TECHNICIAN-SCOPED ONLY)
            ViewBag.TotalAssigned = assignedWorkOrders.Count;
            ViewBag.OpenCount = assignedWorkOrders.Count(w => w.Status == "Open");
            ViewBag.InProgressCount = assignedWorkOrders.Count(w => w.Status == "In Progress");
            ViewBag.CompletedTodayCount = assignedWorkOrders.Count(w => 
                w.Status == "Completed" && 
                w.ActualCompletion.HasValue && 
                w.ActualCompletion.Value.Date == DateTime.Today);

            // Recent assigned work orders (last 5)
            ViewBag.RecentWorkOrders = assignedWorkOrders.Take(5).ToList();

            ViewData["Active"] = "Dashboard";
            return View("TechnicianDashboard");
        }

        /// <summary>
        /// Work Orders page for Technician role only
        /// Shows only work orders assigned to the logged-in technician
        /// </summary>
        [HttpGet]
        [Route("work-orders")]
        public async Task<IActionResult> WorkOrders(string status = "all")
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get current technician's personnel record
            var personnel = await _context.Personnel
                .Where(p => p.CompanyId == companyId && p.UserId == userId)
                .FirstOrDefaultAsync();

            if (personnel == null)
            {
                return Unauthorized();
            }

            // Query only work orders assigned to this technician
            var query = _context.WorkOrders
                .Where(w => w.CompanyId == companyId && 
                           w.AssignedTo == personnel.PersonnelId &&
                           !w.IsArchived)
                .Include(w => w.Asset)
                .Include(w => w.AssignedToPersonnel)
                .Include(w => w.CreatedByPersonnel)
                .Include(w => w.MaintenanceRequest)
                .AsQueryable();

            // Filter by status
            switch (status.ToLower())
            {
                case "open":
                    query = query.Where(w => w.Status == "Open");
                    break;
                case "in-progress":
                case "inprogress":
                    query = query.Where(w => w.Status == "In Progress");
                    break;
                case "completed":
                    query = query.Where(w => w.Status == "Completed");
                    break;
                case "all":
                default:
                    // No status filter - show all active work orders
                    break;
            }

            var workOrders = await query
                .OrderByDescending(w => w.DateCreated)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewData["Active"] = "WorkOrders";
            return View("~/Views/TechnicianDashboard/TechnicianWorkOrders.cshtml", workOrders);
        }

        /// <summary>
        /// Get work order details for technician
        /// </summary>
        [HttpGet]
        [Route("work-orders/{id}")]
        public async Task<IActionResult> GetWorkOrder(int id)
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var personnel = await _context.Personnel
                .Where(p => p.CompanyId == companyId && p.UserId == userId)
                .FirstOrDefaultAsync();

            if (personnel == null)
            {
                return Unauthorized();
            }

            // Ensure work order is assigned to this technician
            var workOrder = await _context.WorkOrders
                .Where(w => w.WorkOrderId == id && 
                           w.CompanyId == companyId && 
                           w.AssignedTo == personnel.PersonnelId)
                .Include(w => w.Asset)
                .Include(w => w.AssignedToPersonnel)
                .Include(w => w.CreatedByPersonnel)
                .Include(w => w.MaintenanceRequest)
                .FirstOrDefaultAsync();

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found or not assigned to you." });
            }

            var result = new
            {
                workOrderId = workOrder.WorkOrderId,
                assetId = workOrder.AssetId,
                assetName = workOrder.Asset?.AssetName,
                description = workOrder.Description,
                assignedTo = workOrder.AssignedTo,
                assignedToName = workOrder.AssignedToPersonnel?.FullName,
                status = workOrder.Status,
                priority = workOrder.Priority,
                createdBy = workOrder.CreatedByPersonnel?.FullName,
                dateCreated = workOrder.DateCreated,
                dueDate = workOrder.DueDate,
                source = workOrder.MaintenanceRequestId.HasValue 
                    ? $"Request #{workOrder.MaintenanceRequest?.RequestNumber}" 
                    : "Manual",
                maintenanceRequestId = workOrder.MaintenanceRequestId
            };

            return Ok(result);
        }

        /// <summary>
        /// Start work on an assigned work order (Open → In Progress)
        /// </summary>
        [HttpPost]
        [Route("work-orders/start/{id}")]
        public async Task<IActionResult> StartWork(int id)
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var personnel = await _context.Personnel
                .Where(p => p.CompanyId == companyId && p.UserId == userId)
                .FirstOrDefaultAsync();

            if (personnel == null)
            {
                return Unauthorized(new { success = false, message = "Personnel record not found." });
            }

            // SECURITY: Enforce CompanyId + Ownership
            var workOrder = await _context.WorkOrders
                .FirstOrDefaultAsync(w => w.WorkOrderId == id && 
                                        w.CompanyId == companyId && 
                                        w.AssignedTo == personnel.PersonnelId);

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found or not assigned to you." });
            }

            if (workOrder.Status != "Open")
            {
                return BadRequest(new { 
                    success = false, 
                    message = $"Cannot start work. Current status is '{workOrder.Status}'. Only 'Open' work orders can be started." 
                });
            }

            if (workOrder.IsArchived)
            {
                return BadRequest(new { success = false, message = "Cannot start archived work orders." });
            }

            workOrder.Status = "In Progress";
            workOrder.DateCreated = DateTime.Now;
            
            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = "Work started successfully!",
                status = workOrder.Status,
                startDate = workOrder.DateCreated
            });
        }

        /// <summary>
        /// Complete an assigned work order (In Progress → Completed)
        /// </summary>
        [HttpPost]
        [Route("work-orders/complete/{id}")]
        public async Task<IActionResult> CompleteWork(int id)
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var personnel = await _context.Personnel
                .Where(p => p.CompanyId == companyId && p.UserId == userId)
                .FirstOrDefaultAsync();

            if (personnel == null)
            {
                return Unauthorized(new { success = false, message = "Personnel record not found." });
            }

            // SECURITY: Enforce CompanyId + Ownership
            var workOrder = await _context.WorkOrders
                .FirstOrDefaultAsync(w => w.WorkOrderId == id && 
                                        w.CompanyId == companyId && 
                                        w.AssignedTo == personnel.PersonnelId);

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found or not assigned to you." });
            }

            if (workOrder.Status != "In Progress")
            {
                return BadRequest(new { 
                    success = false, 
                    message = $"Cannot complete work. Current status is '{workOrder.Status}'. Only 'In Progress' work orders can be completed." 
                });
            }

            if (workOrder.IsArchived)
            {
                return BadRequest(new { success = false, message = "Cannot complete archived work orders." });
            }

            workOrder.Status = "Completed";
            workOrder.ActualCompletion = DateTime.Now;
            
            // LOCK COSTS (recalculate and finalize)
            var finalCost = await _costService.LockCostsAsync(workOrder.WorkOrderId, companyId);
            
            // AUTO-CREATE MAINTENANCE LOG
            var logExists = await _context.MaintenanceLogs
                .AnyAsync(ml => ml.WorkOrderId == workOrder.WorkOrderId);

            if (!logExists)
            {
                var maintenanceLog = new MaintenanceLog
                {
                    CompanyId = workOrder.CompanyId,
                    WorkOrderId = workOrder.WorkOrderId,
                    AssetId = workOrder.AssetId,
                    Title = $"Work Order #{workOrder.WorkOrderId}",
                    Description = workOrder.Description,
                    CompletedByPersonnelId = workOrder.AssignedTo,
                    CompletedDate = workOrder.ActualCompletion.Value,
                    // Cost snapshot
                    LaborCost = finalCost.LaborCost,
                    PartsCost = finalCost.PartsCost,
                    OtherCost = finalCost.OtherCost,
                    TotalCost = finalCost.TotalCost,
                    CreatedAt = DateTime.Now
                };

                _context.MaintenanceLogs.Add(maintenanceLog);
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = "Work order completed successfully!",
                status = workOrder.Status,
                completedDate = workOrder.ActualCompletion
            });
        }
    }
}
