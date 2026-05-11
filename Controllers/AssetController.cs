using IT15_Project.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Services;

namespace IT15_Project.Controllers
{
    [Authorize(Roles = "Owner,Admin,Technician,User")]
    [Route("admin/assets")]
    public class AssetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public AssetController(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index(string search = "", string status = "all")
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var query = _context.Assets
                .Where(a => a.CompanyId == companyId)
                .Include(a => a.Category)
                .AsQueryable();

            // Status filter
            if (status == "active")
                query = query.Where(a => a.Status == AssetStatuses.Active);
            else if (status == "inactive")
                query = query.Where(a => a.Status == AssetStatuses.OutOfService);

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a =>
                    a.AssetName.Contains(search) ||
                    (a.AssetCode != null && a.AssetCode.Contains(search)) ||
                    (a.Location != null && a.Location.Contains(search)));
            }

            var assets = await query.OrderBy(a => a.AssetName).ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewData["Active"] = "Assets";
            return View(assets);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var asset = await _context.Assets
                .Where(a => a.AssetId == id && a.CompanyId == companyId)
                .Include(a => a.Category)
                .FirstOrDefaultAsync();

            if (asset == null)
                return NotFound();

            // Get related data
            var workOrders = await _context.WorkOrders
                .Where(w => w.AssetId == id && w.CompanyId == companyId)
                .Include(w => w.AssignedToPersonnel)
                .OrderByDescending(w => w.DateCreated)
                .ThenByDescending(w => w.WorkOrderId)
                .Take(10)
                .ToListAsync();

            var maintenanceLogs = await _context.MaintenanceLogs
                .Where(ml => ml.AssetId == id && ml.CompanyId == companyId)
                .Include(ml => ml.CompletedByPersonnel)
                .OrderByDescending(ml => ml.CompletedDate)
                .Take(10)
                .ToListAsync();

            var preventiveSchedules = await _context.PreventiveSchedules
                .Where(ps => ps.AssetId == id && ps.CompanyId == companyId)
                .Include(ps => ps.DefaultTechnician)
                .Where(ps => ps.IsActive)
                .ToListAsync();

            ViewBag.WorkOrders = workOrders;
            ViewBag.MaintenanceLogs = maintenanceLogs;
            ViewBag.PreventiveSchedules = preventiveSchedules;
            ViewData["Active"] = "Assets";

            return View(asset);
        }

        [HttpGet]
        [Route("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var categories = await _context.Categories
                    .Where(c => c.CompanyId == companyId)
                    .Select(c => new { value = c.CategoryId, text = c.CategoryName })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load categories", error = ex.Message });
            }
        }

        /// <summary>
        /// Get assets list for dropdowns - accessible by all authenticated users including Requesters
        /// </summary>
        [HttpGet]
        [Route("list")]
        [Authorize(Roles = "Owner,Admin,Technician,User")]
        public async Task<IActionResult> GetAssetsList()
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                
                // LOG: Request received
                var userId = User.Identity?.Name;
                var userRoles = User.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();
                
                Console.WriteLine($"[ASSET LIST] Request received from User: {userId}, CompanyId: {companyId}, Roles: {string.Join(", ", userRoles)}");

                var assets = await _context.Assets
                    .Where(a => a.CompanyId == companyId && a.Status == AssetStatuses.Active)
                    .OrderBy(a => a.AssetName)
                    .Select(a => new { 
                        value = a.AssetId, 
                        text = a.AssetName,
                        code = a.AssetCode,
                        location = a.Location
                    })
                    .ToListAsync();

                Console.WriteLine($"[ASSET LIST] Found {assets.Count} active assets for CompanyId: {companyId}");

                return Ok(assets);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ASSET LIST ERROR] {ex.Message}");
                Console.WriteLine($"[ASSET LIST ERROR] Stack: {ex.StackTrace}");
                
                return StatusCode(500, new { success = false, message = "Failed to load assets", error = ex.Message });
            }
        }

        [HttpPost]
        [Route("create")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateAssetRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Validation failed" });

            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                // Check code uniqueness
                if (!string.IsNullOrEmpty(request.AssetCode))
                {
                    var codeExists = await _context.Assets
                        .AnyAsync(a => a.CompanyId == companyId && a.AssetCode == request.AssetCode);

                    if (codeExists)
                        return BadRequest(new { success = false, message = "Asset code already exists in your company." });
                }

                // Validate category
                if (request.CategoryId.HasValue)
                {
                    var categoryExists = await _context.Categories
                        .AnyAsync(c => c.CategoryId == request.CategoryId.Value && c.CompanyId == companyId);

                    if (!categoryExists)
                        return BadRequest(new { success = false, message = "Invalid category." });
                }

                var asset = new Asset
                {
                    CompanyId = companyId,
                    AssetName = request.AssetName,
                    AssetCode = request.AssetCode,
                    CategoryId = request.CategoryId,
                    Location = request.Location,
                    Description = request.Description,
                    Status = AssetStatuses.Active,
                    CreatedAt = DateTime.Now
                };

                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, assetId = asset.AssetId, message = "Asset created successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpGet]
        [Route("{id}/edit")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> GetAsset(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var asset = await _context.Assets
                    .Where(a => a.AssetId == id && a.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                if (asset == null)
                    return NotFound(new { success = false, message = "Asset not found." });

                return Ok(new
                {
                    assetId = asset.AssetId,
                    assetName = asset.AssetName,
                    assetCode = asset.AssetCode,
                    categoryId = asset.CategoryId,
                    location = asset.Location,
                    description = asset.Description,
                    status = asset.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("{id}/edit")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Edit(int id, [FromBody] EditAssetRequest request)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var asset = await _context.Assets
                    .FirstOrDefaultAsync(a => a.AssetId == id && a.CompanyId == companyId);

                if (asset == null)
                    return NotFound(new { success = false, message = "Asset not found." });

                // Check code uniqueness
                if (!string.IsNullOrEmpty(request.AssetCode) && request.AssetCode != asset.AssetCode)
                {
                    var codeExists = await _context.Assets
                        .AnyAsync(a => a.CompanyId == companyId && a.AssetCode == request.AssetCode && a.AssetId != id);

                    if (codeExists)
                        return BadRequest(new { success = false, message = "Asset code already exists." });
                }

                // Validate category
                if (request.CategoryId.HasValue)
                {
                    var categoryExists = await _context.Categories
                        .AnyAsync(c => c.CategoryId == request.CategoryId.Value && c.CompanyId == companyId);

                    if (!categoryExists)
                        return BadRequest(new { success = false, message = "Invalid category." });
                }

                asset.AssetName = request.AssetName;
                asset.AssetCode = request.AssetCode;
                asset.CategoryId = request.CategoryId;
                asset.Location = request.Location;
                asset.Description = request.Description;
                asset.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Asset updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("{id}/toggle-status")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var asset = await _context.Assets
                    .FirstOrDefaultAsync(a => a.AssetId == id && a.CompanyId == companyId);

                if (asset == null)
                    return NotFound(new { success = false, message = "Asset not found." });

                asset.Status = asset.Status == AssetStatuses.Active ? AssetStatuses.OutOfService : AssetStatuses.Active;
                asset.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, status = asset.Status, message = $"Asset {asset.Status.ToLower()} successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }
    }

    public class CreateAssetRequest
    {
        public string AssetName { get; set; } = string.Empty;
        public string? AssetCode { get; set; }
        public int? CategoryId { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
    }

    public class EditAssetRequest
    {
        public string AssetName { get; set; } = string.Empty;
        public string? AssetCode { get; set; }
        public int? CategoryId { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
    }
}
