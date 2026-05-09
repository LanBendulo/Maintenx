using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Models.ViewModels;
using IT15_Project.Services;
using System.Security.Claims;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// Dashboard for Owner role only. Accessible at /admin/dashboard.
    /// Shows full system metrics, all work orders, maintenance requests, assets.
    /// MULTI-TENANT: All queries filtered by CompanyId
    /// </summary>
    [Authorize(Roles = "Admin,Owner")]
    [Route("admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantService _tenantService;
        private readonly ICostService _costService;

        public DashboardController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ITenantService tenantService,
            ICostService costService)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
            _costService = costService;
        }

        [Route("dashboard")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // TENANT-AWARE: Get current company
                var companyId = _tenantService.GetCurrentCompanyId();

                var viewModel = new AdminDashboardViewModel();

                // ========================================
                // A. SUMMARY CARDS
                // ========================================
                
                // Total Maintenance Requests
                viewModel.TotalRequests = await _context.MaintenanceRequests
                    .AsNoTracking()
                    .Where(mr => mr.CompanyId == companyId)
                    .CountAsync();

                // Pending Requests
                viewModel.PendingRequests = await _context.MaintenanceRequests
                    .AsNoTracking()
                    .Where(mr => mr.CompanyId == companyId && mr.Status == "Pending")
                    .CountAsync();

                // Active Work Orders (Open or In Progress)
                viewModel.ActiveWorkOrders = await _context.WorkOrders
                    .AsNoTracking()
                    .Where(wo => wo.CompanyId == companyId && 
                                (wo.Status == "Open" || wo.Status == "In Progress"))
                    .CountAsync();

                // Total Assets
                viewModel.TotalAssets = await _context.Assets
                    .AsNoTracking()
                    .Where(a => a.CompanyId == companyId && a.Status != "Retired")
                    .CountAsync();

                // ========================================
                // B. REQUEST STATUS CHART
                // ========================================
                
                var requestsByStatus = await _context.MaintenanceRequests
                    .AsNoTracking()
                    .Where(mr => mr.CompanyId == companyId)
                    .GroupBy(mr => mr.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                viewModel.RequestStatusCounts = requestsByStatus.ToDictionary(
                    x => x.Status ?? "Unknown",
                    x => x.Count
                );

                // Ensure all statuses are present (even if 0)
                var allStatuses = new[] { "Pending", "Approved", "Rejected", "Converted" };
                foreach (var status in allStatuses)
                {
                    if (!viewModel.RequestStatusCounts.ContainsKey(status))
                    {
                        viewModel.RequestStatusCounts[status] = 0;
                    }
                }

                // ========================================
                // C. ONGOING WORK ORDERS
                // ========================================
                
                viewModel.OngoingWorkOrders = await _context.WorkOrders
                    .AsNoTracking()
                    .Where(wo => wo.CompanyId == companyId && 
                                (wo.Status == "Open" || wo.Status == "In Progress"))
                    .OrderByDescending(wo => wo.Priority == "High" ? 3 : wo.Priority == "Medium" ? 2 : 1)
                    .ThenByDescending(wo => wo.DateCreated)
                    .Take(10)
                    .Select(wo => new OngoingWorkOrderDto
                    {
                        WorkOrderId = wo.WorkOrderId,
                        WorkOrderNumber = $"WO-{wo.WorkOrderId.ToString().PadLeft(4, '0')}",
                        AssetName = wo.Asset != null ? wo.Asset.AssetName : "N/A",
                        TechnicianName = wo.AssignedToPersonnel != null ? wo.AssignedToPersonnel.FullName : "Unassigned",
                        Priority = wo.Priority ?? "Medium",
                        Status = wo.Status ?? "Open",
                        DueDate = wo.DueDate,
                        ProgressPercentage = wo.Status == "In Progress" ? 50 : 0
                    })
                    .ToListAsync();

                // ========================================
                // D. ACTIVE ALERTS
                // ========================================
                
                var alerts = new List<DashboardAlertDto>();

                // 1. Overdue Work Orders
                var overdueWorkOrders = await _context.WorkOrders
                    .AsNoTracking()
                    .Where(wo => wo.CompanyId == companyId &&
                                wo.DueDate.HasValue &&
                                wo.DueDate.Value < DateTime.Now &&
                                wo.Status != "Completed" &&
                                wo.Status != "Cancelled")
                    .OrderBy(wo => wo.DueDate)
                    .Take(5)
                    .Select(wo => new DashboardAlertDto
                    {
                        Type = "overdue",
                        Title = $"Overdue Work Order #{wo.WorkOrderId}",
                        Description = $"Work order for {(wo.Asset != null ? wo.Asset.AssetName : "Unknown Asset")} is overdue",
                        DueDate = wo.DueDate,
                        AssetOrLocation = wo.Asset != null ? wo.Asset.AssetName : "N/A",
                        Severity = "high"
                    })
                    .ToListAsync();

                alerts.AddRange(overdueWorkOrders);

                // 2. Upcoming Preventive Maintenance (next 7 days)
                var upcomingPM = await _context.PreventiveSchedules
                    .AsNoTracking()
                    .Where(ps => ps.CompanyId == companyId &&
                                ps.IsActive &&
                                ps.NextDueDate >= DateTime.Now &&
                                ps.NextDueDate <= DateTime.Now.AddDays(7))
                    .OrderBy(ps => ps.NextDueDate)
                    .Take(5)
                    .Select(ps => new DashboardAlertDto
                    {
                        Type = "upcoming-pm",
                        Title = $"Upcoming PM: {ps.Title}",
                        Description = $"Preventive maintenance due for {(ps.Asset != null ? ps.Asset.AssetName : "Unknown Asset")}",
                        DueDate = ps.NextDueDate,
                        AssetOrLocation = ps.Asset != null ? ps.Asset.AssetName : "N/A",
                        Severity = "medium"
                    })
                    .ToListAsync();

                alerts.AddRange(upcomingPM);

                // 3. Low Stock Parts
                var lowStockParts = await _context.Parts
                    .AsNoTracking()
                    .Where(p => p.CompanyId == companyId &&
                               p.IsActive &&
                               p.ReorderLevel.HasValue &&
                               p.Quantity <= p.ReorderLevel.Value)
                    .OrderBy(p => p.Quantity)
                    .Take(3)
                    .Select(p => new DashboardAlertDto
                    {
                        Type = "low-stock",
                        Title = $"Low Stock: {p.PartName}",
                        Description = $"Only {p.Quantity} units remaining (reorder at {p.ReorderLevel})",
                        DueDate = null,
                        AssetOrLocation = p.Location ?? "Inventory",
                        Severity = p.Quantity == 0 ? "high" : "medium"
                    })
                    .ToListAsync();

                alerts.AddRange(lowStockParts);

                // Sort alerts by severity and take top 10
                viewModel.Alerts = alerts
                    .OrderByDescending(a => a.Severity == "high" ? 3 : a.Severity == "medium" ? 2 : 1)
                    .ThenBy(a => a.DueDate)
                    .Take(10)
                    .ToList();

                return View("~/Views/Dashboard/Index.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                // Log error and return empty view model
                Console.WriteLine($"Error loading dashboard: {ex.Message}");
                return View("~/Views/Dashboard/Index.cshtml", new AdminDashboardViewModel());
            }
        }

        // Diagnostic endpoint to test database connection
        [Route("test-db")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                // TENANT-AWARE: Filter by current company
                var companyId = _tenantService.GetCurrentCompanyId();
                
                var assetCount = await _context.Assets
                    .Where(a => a.CompanyId == companyId)
                    .CountAsync();
                var categoryCount = await _context.Categories
                    .Where(c => c.CompanyId == companyId)
                    .CountAsync();
                var personnelCount = await _context.Personnel
                    .Where(p => p.CompanyId == companyId)
                    .CountAsync();
                
                return Ok(new {
                    success = true,
                    message = "Database connection successful",
                    companyId = companyId,
                    counts = new {
                        assets = assetCount,
                        categories = categoryCount,
                        personnel = personnelCount
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    success = false,
                    message = "Database connection failed",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [Route("work-orders")]
        public async Task<IActionResult> WorkOrders(string filter = "active")
        {
            // TENANT-AWARE: Filter by current company
            var companyId = _tenantService.GetCurrentCompanyId();
            
            var query = _context.WorkOrders
                .Where(w => w.CompanyId == companyId)  // TENANT FILTER
                .Include(w => w.Asset)
                .Include(w => w.AssignedToPersonnel)
                .Include(w => w.CreatedByPersonnel)
                .Include(w => w.MaintenanceRequest)
                .AsQueryable();

            // Filter based on archive status
            switch (filter.ToLower())
            {
                case "archived":
                    query = query.Where(w => w.IsArchived);
                    break;
                case "all":
                    // No filter - show everything (but still filtered by company)
                    break;
                case "active":
                default:
                    query = query.Where(w => !w.IsArchived);
                    break;
            }

            var workOrders = await query
                .OrderByDescending(w => w.DateCreated)
                .ToListAsync();

            ViewBag.Filter = filter;
            return View("~/Views/Dashboard/WorkOrders.cshtml", workOrders);
        }

        [HttpPost]
        [Route("work-orders/create")]
        public async Task<IActionResult> CreateWorkOrder([FromBody] CreateWorkOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Return structured field-specific errors for AJAX handling
                var fieldErrors = new Dictionary<string, string>();
                
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key]?.Errors;
                    if (errors != null && errors.Count > 0)
                    {
                        var fieldName = key.Split('.').Last(); // Get field name without prefix
                        fieldErrors[fieldName] = errors.First().ErrorMessage;
                    }
                }
                
                return BadRequest(new { 
                    success = false, 
                    errors = fieldErrors,
                    message = "Validation failed. Please check the form fields."
                });
            }

            try
            {
                // TENANT-AWARE: Get current company
                var companyId = _tenantService.GetCurrentCompanyId();
                
                // Get current user's personnel record
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { success = false, message = "User is not authenticated." });
                }
                
                var currentPersonnel = await _context.Personnel
                    .Where(p => p.CompanyId == companyId)  // TENANT FILTER
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (currentPersonnel == null)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Current user does not have a personnel record. UserId: {userId}" 
                    });
                }

                // If linked to a maintenance request, validate it
                if (model.MaintenanceRequestId.HasValue)
                {
                    var request = await _context.MaintenanceRequests
                        .Where(mr => mr.CompanyId == companyId)  // TENANT FILTER
                        .Include(mr => mr.WorkOrder)
                        .FirstOrDefaultAsync(mr => mr.RequestId == model.MaintenanceRequestId.Value);

                    if (request == null)
                    {
                        return BadRequest(new { success = false, message = "Maintenance request not found." });
                    }

                    if (request.Status != "Approved")
                    {
                        return BadRequest(new { success = false, message = "Only approved requests can be converted to work orders." });
                    }

                    if (request.WorkOrder != null)
                    {
                        return BadRequest(new { success = false, message = "This request has already been converted to a work order." });
                    }

                    // Enforce data from request (prevent duplication)
                    model.AssetId = request.AssetId;
                    model.Description = $"{request.Title}\n\n{request.Description}";
                    model.Priority = request.Priority;
                }

                var workOrder = new WorkOrder
                {
                    CompanyId = companyId,  // TENANT ASSIGNMENT
                    AssetId = model.AssetId,
                    AssignedTo = model.AssignedTo,
                    CreatedBy = currentPersonnel.PersonnelId,
                    Status = "Open",
                    Priority = model.Priority,
                    Description = model.Description + (string.IsNullOrEmpty(model.Notes) ? "" : "\n\nNotes: " + model.Notes),
                    DateCreated = model.DateCreated,
                    DueDate = model.DueDate,
                    MaintenanceRequestId = model.MaintenanceRequestId
                };

                _context.WorkOrders.Add(workOrder);

                // If linked to request, update request status to Converted
                if (model.MaintenanceRequestId.HasValue)
                {
                    var request = await _context.MaintenanceRequests
                        .Where(mr => mr.CompanyId == companyId)  // TENANT FILTER
                        .FirstOrDefaultAsync(mr => mr.RequestId == model.MaintenanceRequestId.Value);
                    
                    if (request != null)
                    {
                        request.Status = "Converted";
                        request.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    workOrderId = workOrder.WorkOrderId, 
                    message = model.MaintenanceRequestId.HasValue 
                        ? "Request converted to work order successfully!" 
                        : "Work order created successfully!" 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while creating the work order.", 
                    error = ex.Message,
                    innerError = ex.InnerException?.Message 
                });
            }
        }

        [HttpGet]
        [Route("work-orders/data")]
        public async Task<IActionResult> GetWorkOrdersData()
        {
            // TENANT-AWARE: Filter by current company
            var companyId = _tenantService.GetCurrentCompanyId();
            
            var workOrders = await _context.WorkOrders
                .Where(w => w.CompanyId == companyId)  // TENANT FILTER
                .Include(w => w.Asset)
                .Include(w => w.AssignedToPersonnel)
                .Include(w => w.MaintenanceRequest)
                .OrderByDescending(w => w.DateCreated)
                .Select(w => new
                {
                    workOrderId = w.WorkOrderId,
                    assetName = w.Asset != null ? w.Asset.AssetName : "N/A",
                    assignedTo = w.AssignedToPersonnel != null ? w.AssignedToPersonnel.FullName : "Unassigned",
                    status = w.Status,
                    priority = w.Priority,
                    description = w.Description,
                    dateCreated = w.DateCreated,
                    dueDate = w.DueDate,
                    source = w.MaintenanceRequestId.HasValue ? $"Request #{w.MaintenanceRequest!.RequestNumber}" : "Manual"
                })
                .ToListAsync();

            return Ok(workOrders);
        }

        /// <summary>
        /// Get maintenance request details for conversion to work order
        /// Used when navigating from maintenance requests page with convertRequestId parameter
        /// </summary>
        [HttpGet]
        [Route("work-orders/request-details/{id}")]
        public async Task<IActionResult> GetRequestDetailsForConversion(int id)
        {
            try
            {
                // TENANT-AWARE: Filter by current company
                var companyId = _tenantService.GetCurrentCompanyId();
                
                var request = await _context.MaintenanceRequests
                    .Where(mr => mr.RequestId == id && mr.CompanyId == companyId)  // TENANT FILTER
                    .Include(mr => mr.Asset)
                    .Include(mr => mr.RequestedByPersonnel)
                    .Include(mr => mr.WorkOrder)
                    .FirstOrDefaultAsync();

                if (request == null)
                {
                    return NotFound(new { 
                        success = false, 
                        message = "Maintenance request not found or you don't have permission to access it." 
                    });
                }

                // Check if already converted
                if (request.WorkOrder != null)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "This request has already been converted to a work order.",
                        workOrderId = request.WorkOrder.WorkOrderId
                    });
                }

                // Check if approved
                if (request.Status != "Approved")
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Only approved requests can be converted. Current status: {request.Status}" 
                    });
                }

                // Return request details for conversion
                var result = new
                {
                    success = true,
                    requestId = request.RequestId,
                    requestNumber = request.RequestNumber,
                    title = request.Title,
                    description = request.Description,
                    assetId = request.AssetId,
                    assetName = request.Asset?.AssetName ?? "Unknown Asset",
                    category = request.Category,
                    priority = request.Priority,
                    requesterName = request.RequestedByPersonnel?.FullName ?? "Unknown",
                    createdAt = request.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while loading the request details.", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// REMOVED: Duplicate route - use AssetController.GetAssetsList() at /admin/assets/list instead
        /// This endpoint was causing AmbiguousMatchException with PreventiveMaintenanceController
        /// </summary>

        [HttpGet]
        [Route("technicians/list")]
        public async Task<IActionResult> GetTechnicians()
        {
            try
            {
                // TENANT-AWARE: Filter by current company
                var companyId = _tenantService.GetCurrentCompanyId();
                
                var technicians = await _context.Personnel
                    .Where(p => p.CompanyId == companyId &&  // TENANT FILTER
                                p.IsActive && 
                                (p.Role == "Technician" || p.Role == "Supervisor"))
                    .Select(p => new { 
                        value = p.PersonnelId, 
                        text = p.FullName,
                        skillSet = p.SkillSet,
                        hourlyRate = p.HourlyRate,
                        role = p.Role
                    })
                    .ToListAsync();

                return Ok(technicians);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to load technicians", 
                    error = ex.Message,
                    innerError = ex.InnerException?.Message 
                });
            }
        }

        [HttpGet]
        [Route("maintenance-requests/approved")]
        public async Task<IActionResult> GetApprovedRequests()
        {
            // TENANT-AWARE: Filter by current company
            var companyId = _tenantService.GetCurrentCompanyId();
            
            var requests = await _context.MaintenanceRequests
                .Where(mr => mr.CompanyId == companyId &&  // TENANT FILTER
                             mr.Status == "Approved" && 
                             mr.WorkOrder == null)
                .Include(mr => mr.Asset)
                .Include(mr => mr.WorkOrder)
                .Select(mr => new {
                    value = mr.RequestId,
                    text = $"{mr.RequestNumber} — {mr.Title}",
                    assetId = mr.AssetId,
                    assetName = mr.Asset != null ? mr.Asset.AssetName : "N/A",
                    description = mr.Description,
                    priority = mr.Priority,
                    title = mr.Title
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet]
        [Route("work-orders/{id}")]
        public async Task<IActionResult> GetWorkOrder(int id)
        {
            try
            {
                // TENANT-AWARE: Filter by current company AND id
                var companyId = _tenantService.GetCurrentCompanyId();
                
                var workOrder = await _context.WorkOrders
                    .Where(w => w.WorkOrderId == id && w.CompanyId == companyId)  // TENANT FILTER
                    .Include(w => w.Asset)
                    .Include(w => w.AssignedToPersonnel)
                    .Include(w => w.CreatedByPersonnel)
                    .Include(w => w.MaintenanceRequest)
                    .FirstOrDefaultAsync();

                if (workOrder == null)
                {
                    return NotFound(new { success = false, message = "Work order not found." });
                }

                // Get or create cost record
                var cost = await _costService.GetOrCreateWorkOrderCostAsync(id, companyId);

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
                    maintenanceRequestId = workOrder.MaintenanceRequestId,
                    // Cost data
                    laborCost = cost.LaborCost ?? 0,
                    partsCost = cost.PartsCost ?? 0,
                    otherCost = cost.OtherCost ?? 0,
                    totalCost = cost.TotalCost ?? 0
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        /// <summary>
        /// Update Work Order costs (labor and other)
        /// Only allowed when status is Open or InProgress
        /// </summary>
        [HttpPost]
        [Route("work-orders/{id}/update-cost")]
        public async Task<IActionResult> UpdateWorkOrderCost(int id, [FromBody] UpdateCostRequest request)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                // Validate work order exists and belongs to tenant
                var workOrder = await _context.WorkOrders
                    .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyId == companyId);

                if (workOrder == null)
                {
                    return NotFound(new { success = false, message = "Work order not found." });
                }

                // Validate costs
                if (request.LaborCost < 0 || request.OtherCost < 0)
                {
                    return BadRequest(new { success = false, message = "Costs cannot be negative." });
                }

                // Update costs using service
                var success = await _costService.UpdateCostsAsync(id, companyId, request.LaborCost, request.OtherCost);

                if (!success)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Cannot update costs. Work order must be Open or In Progress." 
                    });
                }

                // Get updated cost
                var cost = await _costService.GetOrCreateWorkOrderCostAsync(id, companyId);

                return Ok(new { 
                    success = true, 
                    message = "Cost updated successfully!",
                    laborCost = cost.LaborCost,
                    partsCost = cost.PartsCost,
                    otherCost = cost.OtherCost,
                    totalCost = cost.TotalCost
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while updating cost.", 
                    error = ex.Message 
                });
            }
        }

        [HttpPut]
        [Route("work-orders/{id}/status")]
        public async Task<IActionResult> UpdateWorkOrderStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                // TENANT-AWARE: Validate ownership before update
                var companyId = _tenantService.GetCurrentCompanyId();
                
                var workOrder = await _context.WorkOrders
                    .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyId == companyId);  // TENANT FILTER

                if (workOrder == null)
                {
                    return NotFound(new { success = false, message = "Work order not found." });
                }

                // Cannot update status of archived work orders
                if (workOrder.IsArchived)
                {
                    return BadRequest(new { success = false, message = "Cannot update status of archived work orders." });
                }

                // Validate status transitions
                var validTransitions = new Dictionary<string, List<string>>
                {
                    { "Open", new List<string> { "In Progress", "Cancelled" } },
                    { "In Progress", new List<string> { "Completed", "Cancelled" } },
                    { "Completed", new List<string>() }, // No transitions from Completed
                    { "Cancelled", new List<string>() }  // No transitions from Cancelled
                };

                var currentStatus = workOrder.Status ?? "Open";
                var newStatus = request.Status;

                if (currentStatus != newStatus)
                {
                    if (!validTransitions.ContainsKey(currentStatus) || 
                        !validTransitions[currentStatus].Contains(newStatus))
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = $"Invalid status transition from '{currentStatus}' to '{newStatus}'. Allowed transitions: {string.Join(", ", validTransitions.GetValueOrDefault(currentStatus, new List<string>()))}" 
                        });
                    }
                }

                // If setting to Completed, require ActualCompletion
                if (newStatus == "Completed")
                {
                    if (!request.ActualCompletion.HasValue)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = "Actual completion date is required when marking as completed." 
                        });
                    }
                    workOrder.ActualCompletion = request.ActualCompletion.Value;

                    // LOCK COSTS (recalculate and finalize)
                    var finalCost = await _costService.LockCostsAsync(workOrder.WorkOrderId, companyId);

                    // AUTO-CREATE MAINTENANCE LOG
                    // Check if log already exists (idempotent)
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
                            CompletedDate = request.ActualCompletion.Value,
                            Notes = request.Notes,
                            // Cost snapshot
                            LaborCost = finalCost.LaborCost,
                            PartsCost = finalCost.PartsCost,
                            OtherCost = finalCost.OtherCost,
                            TotalCost = finalCost.TotalCost,
                            CreatedAt = DateTime.Now
                        };

                        _context.MaintenanceLogs.Add(maintenanceLog);
                    }
                }

                workOrder.Status = newStatus;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Status updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("work-orders/{id}/edit")]
        public async Task<IActionResult> EditWorkOrder(int id, [FromBody] EditWorkOrderRequest request)
        {
            try
            {
                // TENANT-AWARE: Validate ownership before update
                var companyId = _tenantService.GetCurrentCompanyId();
                
                var workOrder = await _context.WorkOrders
                    .Where(w => w.WorkOrderId == id && w.CompanyId == companyId)  // TENANT FILTER
                    .Include(w => w.MaintenanceRequest)
                    .FirstOrDefaultAsync();

                if (workOrder == null)
                {
                    return NotFound(new { success = false, message = "Work order not found." });
                }

                // RULE 1: Cannot edit Completed or Cancelled work orders
                if (workOrder.Status == "Completed" || workOrder.Status == "Cancelled")
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Cannot edit completed or cancelled work orders." 
                    });
                }

                // Cannot edit archived work orders
                if (workOrder.IsArchived)
                {
                    return BadRequest(new { success = false, message = "Cannot edit archived work orders." });
                }

                // RULE 2: If linked to maintenance request, Asset/Description/Priority are READ-ONLY
                if (workOrder.MaintenanceRequestId.HasValue)
                {
                    // Prevent changes to locked fields
                    if (request.AssetId.HasValue && request.AssetId != workOrder.AssetId)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = "Cannot change asset for work orders linked to maintenance requests." 
                        });
                    }
                    
                    if (!string.IsNullOrEmpty(request.Description) && request.Description != workOrder.Description)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = "Cannot change description for work orders linked to maintenance requests." 
                        });
                    }
                    
                    if (!string.IsNullOrEmpty(request.Priority) && request.Priority != workOrder.Priority)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = "Cannot change priority for work orders linked to maintenance requests." 
                        });
                    }
                }
                else
                {
                    // Manual work order - allow editing these fields
                    if (request.AssetId.HasValue)
                    {
                        workOrder.AssetId = request.AssetId.Value;
                    }
                    if (!string.IsNullOrEmpty(request.Description))
                    {
                        workOrder.Description = request.Description;
                    }
                    if (!string.IsNullOrEmpty(request.Priority))
                    {
                        workOrder.Priority = request.Priority;
                    }
                }

                // RULE 3: ExpectedCompletion >= StartDate
                if (request.ExpectedCompletion.HasValue && request.StartDate.HasValue)
                {
                    if (request.ExpectedCompletion.Value < request.StartDate.Value)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = "Expected completion date must be after start date." 
                        });
                    }
                }

                // Update allowed fields (always editable)
                if (request.PersonnelId.HasValue)
                {
                    workOrder.AssignedTo = request.PersonnelId.Value;
                }
                if (request.StartDate.HasValue)
                {
                    workOrder.DateCreated = request.StartDate.Value;
                }
                if (request.ExpectedCompletion.HasValue)
                {
                    workOrder.DueDate = request.ExpectedCompletion.Value;
                }
                if (!string.IsNullOrEmpty(request.Notes))
                {
                    // Append or update notes in description
                    // For simplicity, we'll just update the description if notes are provided
                    // You might want to have a separate Notes field in the database
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    message = "Work order updated successfully!" 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while updating the work order.", 
                    error = ex.Message,
                    innerError = ex.InnerException?.Message 
                });
            }
        }

        [HttpPut]
        [Route("work-orders/{id}/archive")]
        public async Task<IActionResult> ArchiveWorkOrder(int id)
        {
            try
            {
                // TENANT-AWARE: Validate ownership before archive
                var companyId = _tenantService.GetCurrentCompanyId();
                
                var workOrder = await _context.WorkOrders
                    .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyId == companyId);  // TENANT FILTER

                if (workOrder == null)
                {
                    return NotFound(new { success = false, message = "Work order not found." });
                }

                // RULE: Can only archive if status is Completed or Cancelled
                if (workOrder.Status != "Completed" && workOrder.Status != "Cancelled")
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Only completed or cancelled work orders can be archived. Current status: " + workOrder.Status 
                    });
                }

                if (workOrder.IsArchived)
                {
                    return BadRequest(new { success = false, message = "Work order is already archived." });
                }

                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                workOrder.IsArchived = true;
                workOrder.ArchivedAt = DateTime.Now;
                workOrder.ArchivedByUserId = userId;
                
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Work order archived successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("work-orders/{id}/unarchive")]
        public async Task<IActionResult> UnarchiveWorkOrder(int id)
        {
            try
            {
                // TENANT-AWARE: Validate ownership before unarchive
                var companyId = _tenantService.GetCurrentCompanyId();
                
                var workOrder = await _context.WorkOrders
                    .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyId == companyId);  // TENANT FILTER

                if (workOrder == null)
                {
                    return NotFound(new { success = false, message = "Work order not found." });
                }

                if (!workOrder.IsArchived)
                {
                    return BadRequest(new { success = false, message = "Work order is not archived." });
                }

                // Restore from archive
                workOrder.IsArchived = false;
                workOrder.ArchivedAt = null;
                workOrder.ArchivedByUserId = null;
                
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Work order restored successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        // ========================================
        // PARTS MANAGEMENT ENDPOINTS
        // ========================================

        /// <summary>
        /// Get all parts used in a work order
        /// </summary>
        [HttpGet]
        [Route("work-orders/{id}/parts")]
        public async Task<IActionResult> GetWorkOrderParts(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                // Validate work order exists and belongs to tenant
                var workOrder = await _context.WorkOrders
                    .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyId == companyId);

                if (workOrder == null)
                {
                    return NotFound(new { success = false, message = "Work order not found." });
                }

                // Get parts used
                var parts = await _context.WorkOrderParts
                    .Where(wop => wop.WorkOrderId == id && wop.CompanyId == companyId)
                    .Include(wop => wop.Part)
                    .Select(wop => new
                    {
                        id = wop.Id,
                        partId = wop.PartId,
                        partName = wop.Part!.PartName,
                        partNumber = wop.Part.PartNumber,
                        quantityUsed = wop.QuantityUsed,
                        unitCostSnapshot = wop.UnitCost ?? 0,
                        totalCost = wop.QuantityUsed * (wop.UnitCost ?? 0)
                    })
                    .ToListAsync();

                // Calculate total parts cost
                var totalPartsCost = parts.Sum(p => p.totalCost);

                return Ok(new
                {
                    success = true,
                    parts = parts,
                    totalPartsCost = totalPartsCost
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while loading parts.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Add a part to a work order
        /// Deducts from inventory and snapshots unit cost
        /// </summary>
        [HttpPost]
        [Route("work-orders/{id}/add-part")]
        public async Task<IActionResult> AddPartToWorkOrder(int id, [FromBody] AddPartRequest request)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                // Validate work order exists and belongs to tenant
                var workOrder = await _context.WorkOrders
                    .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyId == companyId);

                if (workOrder == null)
                {
                    return NotFound(new { success = false, message = "Work order not found." });
                }

                // Cannot add parts to completed or cancelled work orders
                if (workOrder.Status == "Completed" || workOrder.Status == "Cancelled")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot add parts to completed or cancelled work orders."
                    });
                }

                // Validate part exists and belongs to tenant
                var part = await _context.Parts
                    .FirstOrDefaultAsync(p => p.PartId == request.PartId && p.CompanyId == companyId);

                if (part == null)
                {
                    return NotFound(new { success = false, message = "Part not found." });
                }

                // Validate quantity
                if (request.QuantityUsed <= 0)
                {
                    return BadRequest(new { success = false, message = "Quantity must be greater than 0." });
                }

                // Validate stock availability
                if (part.Quantity < request.QuantityUsed)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Insufficient stock. Available: {part.Quantity}, Requested: {request.QuantityUsed}"
                    });
                }

                // Create WorkOrderPart entry with unit cost snapshot
                var workOrderPart = new WorkOrderPart
                {
                    CompanyId = companyId,
                    WorkOrderId = id,
                    PartId = request.PartId,
                    QuantityUsed = request.QuantityUsed,
                    UnitCost = part.UnitCost ?? 0, // Snapshot current unit cost
                    TotalCost = request.QuantityUsed * (part.UnitCost ?? 0),
                    CreatedAt = DateTime.Now
                };

                _context.WorkOrderParts.Add(workOrderPart);

                // Deduct from inventory
                part.Quantity -= request.QuantityUsed;
                part.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Recalculate parts cost using CostService
                var updatedCost = await _costService.GetOrCreateWorkOrderCostAsync(id, companyId);

                return Ok(new
                {
                    success = true,
                    message = "Part added successfully!",
                    workOrderPartId = workOrderPart.Id,
                    partsCost = updatedCost.PartsCost,
                    totalCost = updatedCost.TotalCost
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while adding part.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Remove a part from a work order
        /// Restores inventory
        /// </summary>
        [HttpPost]
        [Route("work-orders/{id}/remove-part")]
        public async Task<IActionResult> RemovePartFromWorkOrder(int id, [FromBody] RemovePartRequest request)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                // Validate work order exists and belongs to tenant
                var workOrder = await _context.WorkOrders
                    .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyId == companyId);

                if (workOrder == null)
                {
                    return NotFound(new { success = false, message = "Work order not found." });
                }

                // Cannot remove parts from completed or cancelled work orders
                if (workOrder.Status == "Completed" || workOrder.Status == "Cancelled")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot remove parts from completed or cancelled work orders."
                    });
                }

                // Validate WorkOrderPart exists and belongs to tenant
                var workOrderPart = await _context.WorkOrderParts
                    .Include(wop => wop.Part)
                    .FirstOrDefaultAsync(wop => wop.Id == request.WorkOrderPartId && 
                                               wop.CompanyId == companyId &&
                                               wop.WorkOrderId == id);

                if (workOrderPart == null)
                {
                    return NotFound(new { success = false, message = "Part usage record not found." });
                }

                // Restore inventory
                if (workOrderPart.Part != null)
                {
                    workOrderPart.Part.Quantity += workOrderPart.QuantityUsed;
                    workOrderPart.Part.UpdatedAt = DateTime.Now;
                }

                // Remove WorkOrderPart entry
                _context.WorkOrderParts.Remove(workOrderPart);
                await _context.SaveChangesAsync();

                // Recalculate parts cost using CostService
                var updatedCost = await _costService.GetOrCreateWorkOrderCostAsync(id, companyId);

                return Ok(new
                {
                    success = true,
                    message = "Part removed successfully!",
                    partsCost = updatedCost.PartsCost,
                    totalCost = updatedCost.TotalCost
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while removing part.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get available parts from inventory for selection
        /// </summary>
        [HttpGet]
        [Route("parts/available")]
        public async Task<IActionResult> GetAvailableParts()
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var parts = await _context.Parts
                    .Where(p => p.CompanyId == companyId && p.IsActive && p.Quantity > 0)
                    .Select(p => new
                    {
                        value = p.PartId,
                        text = p.PartName + (p.PartNumber != null ? $" ({p.PartNumber})" : ""),
                        partName = p.PartName,
                        partNumber = p.PartNumber,
                        availableQuantity = p.Quantity,
                        unitCost = p.UnitCost ?? 0,
                        location = p.Location
                    })
                    .OrderBy(p => p.partName)
                    .ToListAsync();

                return Ok(parts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to load parts",
                    error = ex.Message
                });
            }
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public DateTime? ActualCompletion { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateCostRequest
    {
        public decimal LaborCost { get; set; }
        public decimal OtherCost { get; set; }
    }

    public class EditWorkOrderRequest
    {
        public int? AssetId { get; set; }
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public int? PersonnelId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpectedCompletion { get; set; }
        public string? Notes { get; set; }
    }

    public class AddPartRequest
    {
        public int PartId { get; set; }
        public int QuantityUsed { get; set; }
    }

    public class RemovePartRequest
    {
        public int WorkOrderPartId { get; set; }
    }
}