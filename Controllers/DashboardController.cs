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

        public DashboardController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ITenantService tenantService)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
        }

        [Route("dashboard")]
        public IActionResult Index()
        {
            return View("~/Views/Dashboard/Index.cshtml");
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
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(new { 
                    success = false, 
                    errors = errors,
                    message = "Validation failed: " + string.Join(", ", errors)
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

        [HttpGet]
        [Route("assets/list")]
        public async Task<IActionResult> GetAssets()
        {
            try
            {
                // TENANT-AWARE: Filter by current company
                var companyId = _tenantService.GetCurrentCompanyId();
                
                var assets = await _context.Assets
                    .Where(a => a.CompanyId == companyId && a.Status != "Retired")  // TENANT FILTER
                    .Select(a => new { value = a.AssetId, text = a.AssetName })
                    .ToListAsync();
                
                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to load assets", 
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

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
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
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
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public DateTime? ActualCompletion { get; set; }
        public string? Notes { get; set; }
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
}