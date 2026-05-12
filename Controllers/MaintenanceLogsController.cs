using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Data;
using IT15_Project.Services;
using System.Security.Claims;

namespace IT15_Project.Controllers
{
    [Authorize(Roles = "Owner,Admin,Supervisor,Technician")]
    public class MaintenanceLogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public MaintenanceLogsController(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        // Owner/Admin route
        [HttpGet]
        [Route("/admin/maintenance-logs")]
        [Authorize(Roles = "Owner,Admin,Supervisor")]
        public async Task<IActionResult> AdminIndex(string search = "")
        {
            var result = await Index(search);
            return View("Index", ((ViewResult)result).Model);
        }

        // Technician route
        [HttpGet]
        [Route("/maintenance-logs")]
        [Authorize(Roles = "Technician")]
        public async Task<IActionResult> TechnicianIndex(string search = "")
        {
            var result = await Index(search);
            return View("Index", ((ViewResult)result).Model);
        }

        private async Task<IActionResult> Index(string search = "")
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var query = _context.MaintenanceLogs
                .Where(ml => ml.CompanyId == companyId)
                .Include(ml => ml.Asset)
                .Include(ml => ml.CompletedByPersonnel)
                .Include(ml => ml.WorkOrder)
                .AsQueryable();

            // Role-based filtering
            if (userRole == "Technician")
            {
                var currentPersonnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.UserId == userId);

                if (currentPersonnel != null)
                {
                    query = query.Where(ml => ml.CompletedByPersonnelId == currentPersonnel.PersonnelId);
                }
            }

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

        // Owner/Admin details route
        [HttpGet]
        [Route("/admin/maintenance-logs/{id}")]
        [Authorize(Roles = "Owner,Admin,Supervisor")]
        public async Task<IActionResult> AdminDetails(int id)
        {
            return await Details(id);
        }

        // Technician details route
        [HttpGet]
        [Route("/maintenance-logs/{id}")]
        [Authorize(Roles = "Technician")]
        public async Task<IActionResult> TechnicianDetails(int id)
        {
            return await Details(id);
        }

        private async Task<IActionResult> Details(int id)
        {
            var companyId = _tenantService.GetCurrentCompanyId();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var query = _context.MaintenanceLogs
                .Where(ml => ml.LogId == id && ml.CompanyId == companyId)
                .Include(ml => ml.Asset)
                .Include(ml => ml.CompletedByPersonnel)
                .Include(ml => ml.WorkOrder)
                .AsQueryable();

            // Role-based filtering
            if (userRole == "Technician")
            {
                var currentPersonnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.UserId == userId);

                if (currentPersonnel != null)
                {
                    query = query.Where(ml => ml.CompletedByPersonnelId == currentPersonnel.PersonnelId);
                }
            }

            var log = await query.FirstOrDefaultAsync();

            if (log == null)
            {
                return NotFound();
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
