using IT15_Project.Constants;
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
        private readonly AssetStatusService _assetStatusService;
        private readonly Services.Parts.IPartsService _partsService;

        public TechnicianDashboardController(
            ApplicationDbContext context, 
            ITenantService tenantService,
            ICostService costService,
            AssetStatusService assetStatusService,
            Services.Parts.IPartsService partsService)
        {
            _context = context;
            _tenantService = tenantService;
            _costService = costService;
            _assetStatusService = assetStatusService;
            _partsService = partsService;
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
                TempData["ErrorMessage"] = "Your account is not linked to a personnel record. Please contact your administrator.";
                return RedirectToAction("Index", "Home");
            }

            // Get assigned work orders (TECHNICIAN-SCOPED)
            var assignedWorkOrders = await _context.WorkOrders
                .Where(w => w.CompanyId == companyId && 
                           w.AssignedTo == personnel.PersonnelId &&
                           !w.IsArchived)
                .Include(w => w.Asset)
                .OrderByDescending(w => w.DateCreated)
                .ThenByDescending(w => w.WorkOrderId)
                .ToListAsync();

            // Calculate metrics (TECHNICIAN-SCOPED ONLY)
            ViewBag.TotalAssigned = assignedWorkOrders.Count;
            ViewBag.PendingCount = assignedWorkOrders.Count(w => w.Status == WorkOrderStatuses.Pending);
            ViewBag.InProgressCount = assignedWorkOrders.Count(w => w.Status == WorkOrderStatuses.InProgress);
            ViewBag.CompletedTodayCount = assignedWorkOrders.Count(w => 
                w.Status == WorkOrderStatuses.Completed && 
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
                TempData["ErrorMessage"] = "Your account is not linked to a personnel record. Please contact your administrator.";
                return RedirectToAction("Index", "Home");
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
                case "pending":
                    query = query.Where(w => w.Status == WorkOrderStatuses.Pending);
                    break;
                case "in-progress":
                case "inprogress":
                    query = query.Where(w => w.Status == WorkOrderStatuses.InProgress);
                    break;
                case "completed":
                    query = query.Where(w => w.Status == WorkOrderStatuses.Completed);
                    break;
                case "all":
                default:
                    // No status filter - show all active work orders
                    break;
            }

            var workOrders = await query
                .OrderByDescending(w => w.DateCreated)
                .ThenByDescending(w => w.WorkOrderId)
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
                TempData["ErrorMessage"] = "Your account is not linked to a personnel record. Please contact your administrator.";
                return RedirectToAction("Index", "Home");
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
        /// Start work on an assigned work order (Pending → In Progress)
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

            if (!WorkOrderStatuses.CanStart(workOrder.Status))
            {
                return BadRequest(new { 
                    success = false, 
                    message = $"Cannot start work. Current status is '{workOrder.Status}'. Only 'Pending' work orders can be started." 
                });
            }

            if (workOrder.IsArchived)
            {
                return BadRequest(new { success = false, message = "Cannot start archived work orders." });
            }

            workOrder.Status = WorkOrderStatuses.InProgress;
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

            if (!WorkOrderStatuses.CanComplete(workOrder.Status))
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

            workOrder.Status = WorkOrderStatuses.Completed;
            workOrder.ActualCompletion = DateTime.Now;
            
            // CONSUME STAGED PARTS (finalize inventory deduction)
            if (userId != null)
            {
                var partsResult = await _partsService.ConsumeWorkOrderPartsAsync(
                    workOrder.WorkOrderId, 
                    userId, 
                    companyId);
                
                if (!partsResult.Success)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Work order completed but parts consumption failed: {partsResult.Message}" 
                    });
                }
            }
            
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

            // ASSET STATUS UPDATE: Return asset to Active if no other active work orders
            await _assetStatusService.OnWorkOrderCompletedOrCancelledAsync(workOrder.WorkOrderId, userId);

            return Ok(new { 
                success = true, 
                message = "Work order completed successfully!",
                status = workOrder.Status,
                completedDate = workOrder.ActualCompletion
            });
        }

        // ============================================================
        // PARTS MANAGEMENT ENDPOINTS
        // ============================================================

        /// <summary>
        /// Get staged parts for a work order
        /// Technician can only access parts for assigned work orders
        /// </summary>
        [HttpGet]
        [Route("work-orders/{id}/parts")]
        public async Task<IActionResult> GetWorkOrderParts(int id)
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

            // Verify work order is assigned to this technician
            var workOrder = await _context.WorkOrders
                .FirstOrDefaultAsync(w => w.WorkOrderId == id && 
                                        w.CompanyId == companyId && 
                                        w.AssignedTo == personnel.PersonnelId);

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found or not assigned to you." });
            }

            // Get staged parts (costs hidden from technicians)
            var parts = await _partsService.GetStagedPartsAsync(id, companyId);

            var result = parts.Select(p => new
            {
                id = p.Id,
                partId = p.PartId,
                partName = p.Part?.PartName,
                partNumber = p.Part?.PartNumber,
                quantityUsed = p.QuantityUsed,
                usageStatus = p.UsageStatus,
                addedBy = p.AddedByPersonnel?.FullName,
                createdAt = p.CreatedAt,
                updatedAt = p.UpdatedAt,
                canEdit = p.UsageStatus == "Pending" && 
                         (workOrder.Status == WorkOrderStatuses.Pending || 
                          workOrder.Status == WorkOrderStatuses.InProgress)
            }).ToList();

            return Ok(new { success = true, parts = result });
        }

        /// <summary>
        /// Get available parts for selection
        /// Costs are hidden from technicians
        /// </summary>
        [HttpGet]
        [Route("parts/available")]
        public async Task<IActionResult> GetAvailableParts()
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Get parts without cost information (includeCost: false)
            var parts = await _partsService.GetAvailablePartsAsync(companyId, includeCost: false);

            var result = parts.Select(p => new
            {
                partId = p.PartId,
                partName = p.PartName,
                partNumber = p.PartNumber,
                description = p.Description,
                quantity = p.Quantity,
                location = p.Location
            }).ToList();

            return Ok(new { success = true, parts = result });
        }

        /// <summary>
        /// Stage a part for use in a work order
        /// Validates stock but does NOT deduct inventory
        /// </summary>
        [HttpPost]
        [Route("work-orders/{id}/add-part")]
        public async Task<IActionResult> AddPart(int id, [FromBody] AddPartRequest request)
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

            // Verify work order is assigned to this technician
            var workOrder = await _context.WorkOrders
                .FirstOrDefaultAsync(w => w.WorkOrderId == id && 
                                        w.CompanyId == companyId && 
                                        w.AssignedTo == personnel.PersonnelId);

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found or not assigned to you." });
            }

            // Validate work order status
            if (workOrder.Status == WorkOrderStatuses.Completed || 
                workOrder.Status == WorkOrderStatuses.Cancelled)
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Cannot add parts to completed or cancelled work orders." 
                });
            }

            // Stage the part
            var result = await _partsService.StagePartUsageAsync(
                id,
                request.PartId,
                request.Quantity,
                personnel.PersonnelId,
                companyId);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { 
                success = true, 
                message = result.Message,
                partId = result.Part?.Id
            });
        }

        /// <summary>
        /// Remove staged part from work order
        /// Only allowed for Pending parts on active work orders
        /// </summary>
        [HttpPost]
        [Route("work-orders/{workOrderId}/remove-part/{partId}")]
        public async Task<IActionResult> RemovePart(int workOrderId, int partId)
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

            // Verify work order is assigned to this technician
            var workOrder = await _context.WorkOrders
                .FirstOrDefaultAsync(w => w.WorkOrderId == workOrderId && 
                                        w.CompanyId == companyId && 
                                        w.AssignedTo == personnel.PersonnelId);

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found or not assigned to you." });
            }

            // Remove the staged part
            var result = await _partsService.RemoveStagedPartAsync(
                partId,
                personnel.PersonnelId,
                companyId);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// Update staged part quantity
        /// Only allowed for Pending parts on active work orders
        /// </summary>
        [HttpPut]
        [Route("work-orders/{workOrderId}/parts/{partId}")]
        public async Task<IActionResult> UpdatePartQuantity(int workOrderId, int partId, [FromBody] UpdatePartQuantityRequest request)
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

            // Verify work order is assigned to this technician
            var workOrder = await _context.WorkOrders
                .FirstOrDefaultAsync(w => w.WorkOrderId == workOrderId && 
                                        w.CompanyId == companyId && 
                                        w.AssignedTo == personnel.PersonnelId);

            if (workOrder == null)
            {
                return NotFound(new { success = false, message = "Work order not found or not assigned to you." });
            }

            // Update the quantity
            var result = await _partsService.UpdateStagedPartQuantityAsync(
                partId,
                request.Quantity,
                personnel.PersonnelId,
                companyId);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }

        // ============================================================
        // REQUEST MODELS
        // ============================================================

        public class AddPartRequest
        {
            public int PartId { get; set; }
            public int Quantity { get; set; }
        }

        public class UpdatePartQuantityRequest
        {
            public int Quantity { get; set; }
        }
    }
}
