using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Data;
using IT15_Project.Services;
using System.Security.Claims;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// Dashboard for User (Requester) role only. Accessible at /userdashboard.
    /// Shows ONLY user's own maintenance requests and request-related data.
    /// MULTI-TENANT: All queries filtered by CompanyId AND RequestedBy (user ownership)
    /// </summary>
    [Authorize(Roles = "User")]
    [Route("userdashboard")]
    public class UserDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public UserDashboardController(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        /// <summary>
        /// User Dashboard - Shows only user's own maintenance requests
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get current user's personnel record
            var personnel = await _context.Personnel
                .Where(p => p.CompanyId == companyId && p.UserId == userId)
                .FirstOrDefaultAsync();

            if (personnel == null)
            {
                return Unauthorized();
            }

            // Get user's maintenance requests (USER-SCOPED)
            var myRequests = await _context.MaintenanceRequests
                .Where(mr => mr.CompanyId == companyId && 
                            mr.RequestedBy == personnel.PersonnelId &&
                            !mr.IsArchived)
                .Include(mr => mr.Asset)
                .Include(mr => mr.WorkOrder)
                .OrderByDescending(mr => mr.CreatedAt)
                .ToListAsync();

            // Calculate metrics (USER-SCOPED ONLY)
            ViewBag.TotalRequests = myRequests.Count;
            ViewBag.PendingCount = myRequests.Count(mr => mr.Status == "Pending");
            ViewBag.ApprovedCount = myRequests.Count(mr => mr.Status == "Approved");
            ViewBag.ConvertedCount = myRequests.Count(mr => mr.Status == "Converted");

            // Recent requests (last 5)
            ViewBag.RecentRequests = myRequests.Take(5).ToList();

            ViewData["Active"] = "Dashboard";
            return View("UserDashboard");
        }

        /// <summary>
        /// Maintenance Requests page for User role only
        /// Shows only requests created by the logged-in user
        /// </summary>
        [HttpGet]
        [Route("maintenance-requests")]
        public async Task<IActionResult> MaintenanceRequests(string status = "all")
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get current user's personnel record
            var personnel = await _context.Personnel
                .Where(p => p.CompanyId == companyId && p.UserId == userId)
                .FirstOrDefaultAsync();

            if (personnel == null)
            {
                return Unauthorized();
            }

            // Query only requests created by this user
            var query = _context.MaintenanceRequests
                .Where(mr => mr.CompanyId == companyId && 
                            mr.RequestedBy == personnel.PersonnelId &&
                            !mr.IsArchived)
                .Include(mr => mr.Asset)
                .Include(mr => mr.WorkOrder)
                .AsQueryable();

            // Filter by status
            switch (status.ToLower())
            {
                case "pending":
                    query = query.Where(mr => mr.Status == "Pending");
                    break;
                case "approved":
                    query = query.Where(mr => mr.Status == "Approved");
                    break;
                case "rejected":
                    query = query.Where(mr => mr.Status == "Rejected");
                    break;
                case "converted":
                    query = query.Where(mr => mr.Status == "Converted");
                    break;
                case "all":
                default:
                    // No status filter - show all active requests
                    break;
            }

            var requests = await query
                .OrderByDescending(mr => mr.CreatedAt)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            ViewData["Active"] = "MaintenanceRequests";
            return View("UserMaintenanceRequests", requests);
        }

        /// <summary>
        /// Get maintenance request details for user
        /// </summary>
        [HttpGet]
        [Route("maintenance-requests/{id}")]
        public async Task<IActionResult> GetMaintenanceRequest(int id)
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

            // Ensure request was created by this user
            var request = await _context.MaintenanceRequests
                .Where(mr => mr.RequestId == id && 
                            mr.CompanyId == companyId && 
                            mr.RequestedBy == personnel.PersonnelId)
                .Include(mr => mr.Asset)
                .Include(mr => mr.WorkOrder)
                .FirstOrDefaultAsync();

            if (request == null)
            {
                return NotFound(new { success = false, message = "Request not found or not created by you." });
            }

            var result = new
            {
                requestId = request.RequestId,
                requestNumber = request.RequestNumber,
                title = request.Title,
                description = request.Description,
                assetId = request.AssetId,
                assetName = request.Asset?.AssetName,
                category = request.Category,
                priority = request.Priority,
                status = request.Status,
                createdAt = request.CreatedAt,
                workOrderId = request.WorkOrder?.WorkOrderId,
                workOrderStatus = request.WorkOrder?.Status
            };

            return Ok(result);
        }
    }
}
