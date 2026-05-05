using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Models.ViewModels;
using System.Security.Claims;

namespace IT15_Project.Controllers
{
    [Authorize(Roles = "Admin,Manager,Requester")]
    [Route("admin/maintenance-requests")]
    public class MaintenanceRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceRequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index(string filter = "active")
        {
            var query = _context.MaintenanceRequests
                .Include(mr => mr.Asset)
                .Include(mr => mr.RequestedByPersonnel)
                .Include(mr => mr.WorkOrder)
                .AsQueryable();

            // Filter based on archive status
            switch (filter.ToLower())
            {
                case "archived":
                    query = query.Where(mr => mr.IsArchived);
                    break;
                case "all":
                    // No filter - show everything
                    break;
                case "active":
                default:
                    query = query.Where(mr => !mr.IsArchived);
                    break;
            }

            var requests = await query
                .OrderByDescending(mr => mr.CreatedAt)
                .ToListAsync();

            ViewBag.Filter = filter;
            return View("~/Views/MaintenanceRequests/Index.cshtml", requests);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromForm] CreateMaintenanceRequestViewModel model)
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
                // Get current user's personnel record
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentPersonnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                // Generate request number
                var lastRequest = await _context.MaintenanceRequests
                    .OrderByDescending(mr => mr.RequestId)
                    .FirstOrDefaultAsync();
                
                var nextNumber = (lastRequest?.RequestId ?? 0) + 1;
                var requestNumber = $"MR-{nextNumber:D4}";

                // Handle file upload
                string? attachmentUrl = null;
                if (model.Attachment != null && model.Attachment.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "maintenance-requests");
                    Directory.CreateDirectory(uploadsFolder);
                    
                    var uniqueFileName = $"{Guid.NewGuid()}_{model.Attachment.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Attachment.CopyToAsync(fileStream);
                    }
                    
                    attachmentUrl = $"/uploads/maintenance-requests/{uniqueFileName}";
                }

                var maintenanceRequest = new MaintenanceRequest
                {
                    RequestNumber = requestNumber,
                    Title = model.Title,
                    Description = model.Description,
                    AssetId = model.AssetId,
                    Priority = model.Priority,
                    Status = "Pending",
                    RequestedBy = currentPersonnel?.PersonnelId,
                    Category = model.Category,
                    Location = model.Location,
                    AttachmentUrl = attachmentUrl,
                    CreatedAt = DateTime.Now
                };

                _context.MaintenanceRequests.Add(maintenanceRequest);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    requestId = maintenanceRequest.RequestId,
                    requestNumber = maintenanceRequest.RequestNumber,
                    message = "Maintenance request created successfully!" 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while creating the maintenance request.", 
                    error = ex.Message,
                    innerError = ex.InnerException?.Message 
                });
            }
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            try
            {
                var request = await _context.MaintenanceRequests
                    .Include(mr => mr.Asset)
                    .Include(mr => mr.RequestedByPersonnel)
                    .Include(mr => mr.WorkOrder)
                    .FirstOrDefaultAsync(mr => mr.RequestId == id);

                if (request == null)
                {
                    return NotFound(new { success = false, message = "Maintenance request not found." });
                }

                var result = new
                {
                    requestId = request.RequestId,
                    requestNumber = request.RequestNumber,
                    title = request.Title,
                    description = request.Description,
                    assetName = request.Asset?.AssetName,
                    priority = request.Priority,
                    status = request.Status,
                    requestedBy = request.RequestedByPersonnel?.FullName,
                    category = request.Category,
                    location = request.Location,
                    attachmentUrl = request.AttachmentUrl,
                    createdAt = request.CreatedAt,
                    updatedAt = request.UpdatedAt,
                    workOrderId = request.WorkOrder?.WorkOrderId
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("{id}/approve")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var request = await _context.MaintenanceRequests.FindAsync(id);

                if (request == null)
                {
                    return NotFound(new { success = false, message = "Maintenance request not found." });
                }

                if (request.Status != "Pending")
                {
                    return BadRequest(new { success = false, message = "Only pending requests can be approved." });
                }

                request.Status = "Approved";
                request.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Request approved successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("{id}/reject")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var request = await _context.MaintenanceRequests.FindAsync(id);

                if (request == null)
                {
                    return NotFound(new { success = false, message = "Maintenance request not found." });
                }

                if (request.Status != "Pending")
                {
                    return BadRequest(new { success = false, message = "Only pending requests can be rejected." });
                }

                request.Status = "Rejected";
                request.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Request rejected successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPost]
        [Route("{id}/convert")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ConvertToWorkOrder(int id)
        {
            try
            {
                var request = await _context.MaintenanceRequests
                    .Include(mr => mr.WorkOrder)
                    .FirstOrDefaultAsync(mr => mr.RequestId == id);

                if (request == null)
                {
                    return NotFound(new { success = false, message = "Maintenance request not found." });
                }

                if (request.Status != "Approved")
                {
                    return BadRequest(new { success = false, message = "Only approved requests can be converted to work orders." });
                }

                if (request.WorkOrder != null)
                {
                    return BadRequest(new { success = false, message = "This request has already been converted to a work order." });
                }

                // Get current user's personnel record
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentPersonnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (currentPersonnel == null)
                {
                    return BadRequest(new { success = false, message = "Current user does not have a personnel record." });
                }

                // Create work order from maintenance request
                var workOrder = new WorkOrder
                {
                    AssetId = request.AssetId,
                    Description = $"{request.Title}\n\n{request.Description}",
                    Priority = request.Priority,
                    Status = "Pending",
                    CreatedBy = currentPersonnel.PersonnelId,
                    DateCreated = DateTime.Now,
                    MaintenanceRequestId = request.RequestId
                };

                _context.WorkOrders.Add(workOrder);

                // Update maintenance request status
                request.Status = "Converted";
                request.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    workOrderId = workOrder.WorkOrderId,
                    message = "Request converted to work order successfully!" 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while converting the request.", 
                    error = ex.Message,
                    innerError = ex.InnerException?.Message 
                });
            }
        }

        [HttpGet]
        [Route("pending-count")]
        public async Task<IActionResult> GetPendingCount()
        {
            try
            {
                var count = await _context.MaintenanceRequests
                    .CountAsync(mr => mr.Status == "Pending" && !mr.IsArchived);

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("{id}/archive")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Archive(int id)
        {
            try
            {
                var request = await _context.MaintenanceRequests.FindAsync(id);

                if (request == null)
                {
                    return NotFound(new { success = false, message = "Maintenance request not found." });
                }

                // RULE: Can only archive if status is Rejected or Converted
                if (request.Status != "Rejected" && request.Status != "Converted")
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Only rejected or converted requests can be archived. Current status: " + request.Status 
                    });
                }

                if (request.IsArchived)
                {
                    return BadRequest(new { success = false, message = "Request is already archived." });
                }

                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                request.IsArchived = true;
                request.ArchivedAt = DateTime.Now;
                request.ArchivedByUserId = userId;
                request.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Request archived successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("{id}/unarchive")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Unarchive(int id)
        {
            try
            {
                var request = await _context.MaintenanceRequests.FindAsync(id);

                if (request == null)
                {
                    return NotFound(new { success = false, message = "Maintenance request not found." });
                }

                if (!request.IsArchived)
                {
                    return BadRequest(new { success = false, message = "Request is not archived." });
                }

                // Restore from archive
                request.IsArchived = false;
                request.ArchivedAt = null;
                request.ArchivedByUserId = null;
                request.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Request restored successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }
    }
}
