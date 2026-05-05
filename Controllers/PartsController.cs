using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Services;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// Parts Inventory Management Controller
    /// Owner/Admin only - manages spare parts inventory
    /// MULTI-TENANT: All queries filtered by CompanyId
    /// </summary>
    [Authorize(Roles = "Owner,Admin")]
    [Route("admin/parts")]
    public class PartsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public PartsController(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        /// <summary>
        /// Parts Inventory Index - List all parts with filters
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index(string search = "", string status = "all", bool lowStock = false)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Base query with tenant filtering
            var query = _context.Parts
                .Where(p => p.CompanyId == companyId)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p => 
                    p.PartName.ToLower().Contains(search) ||
                    (p.PartNumber != null && p.PartNumber.ToLower().Contains(search)) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)));
            }

            if (status == "active")
            {
                query = query.Where(p => p.IsActive);
            }
            else if (status == "inactive")
            {
                query = query.Where(p => !p.IsActive);
            }

            if (lowStock)
            {
                query = query.Where(p => p.ReorderLevel.HasValue && p.Quantity <= p.ReorderLevel.Value);
            }

            var parts = await query
                .OrderBy(p => p.PartName)
                .ToListAsync();

            // Calculate summary metrics
            var allParts = await _context.Parts
                .Where(p => p.CompanyId == companyId)
                .ToListAsync();

            ViewBag.TotalParts = allParts.Count;
            ViewBag.ActiveParts = allParts.Count(p => p.IsActive);
            ViewBag.InactiveParts = allParts.Count(p => !p.IsActive);
            ViewBag.LowStockParts = allParts.Count(p => p.ReorderLevel.HasValue && p.Quantity <= p.ReorderLevel.Value);

            ViewBag.SearchFilter = search;
            ViewBag.StatusFilter = status;
            ViewBag.LowStockFilter = lowStock;

            ViewData["Active"] = "Parts";
            return View(parts);
        }

        /// <summary>
        /// Get part details by ID (JSON)
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetPart(int id)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var part = await _context.Parts
                .Where(p => p.PartId == id && p.CompanyId == companyId)
                .FirstOrDefaultAsync();

            if (part == null)
            {
                return NotFound(new { success = false, message = "Part not found." });
            }

            var result = new
            {
                partId = part.PartId,
                partName = part.PartName,
                partNumber = part.PartNumber,
                description = part.Description,
                quantity = part.Quantity,
                unitCost = part.UnitCost,
                reorderLevel = part.ReorderLevel,
                location = part.Location,
                isActive = part.IsActive,
                createdAt = part.CreatedAt,
                updatedAt = part.UpdatedAt
            };

            return Ok(result);
        }

        /// <summary>
        /// Create new part (POST)
        /// </summary>
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] Part model)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Validation
            if (string.IsNullOrWhiteSpace(model.PartName))
            {
                return BadRequest(new { success = false, message = "Part name is required." });
            }

            if (model.UnitCost.HasValue && model.UnitCost.Value < 0)
            {
                return BadRequest(new { success = false, message = "Unit cost cannot be negative." });
            }

            if (model.Quantity < 0)
            {
                return BadRequest(new { success = false, message = "Quantity cannot be negative." });
            }

            // Check for duplicate part number within company
            if (!string.IsNullOrWhiteSpace(model.PartNumber))
            {
                var duplicate = await _context.Parts
                    .AnyAsync(p => p.CompanyId == companyId && 
                                  p.PartNumber == model.PartNumber);

                if (duplicate)
                {
                    return BadRequest(new { success = false, message = "Part number already exists in your inventory." });
                }
            }

            var part = new Part
            {
                CompanyId = companyId,
                PartName = model.PartName.Trim(),
                PartNumber = model.PartNumber?.Trim(),
                Description = model.Description?.Trim(),
                Quantity = model.Quantity,
                UnitCost = model.UnitCost,
                ReorderLevel = model.ReorderLevel,
                Location = model.Location?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Parts.Add(part);
            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = "Part created successfully!",
                partId = part.PartId
            });
        }

        /// <summary>
        /// Update existing part (PUT)
        /// </summary>
        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Part model)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var part = await _context.Parts
                .FirstOrDefaultAsync(p => p.PartId == id && p.CompanyId == companyId);

            if (part == null)
            {
                return NotFound(new { success = false, message = "Part not found." });
            }

            // Validation
            if (string.IsNullOrWhiteSpace(model.PartName))
            {
                return BadRequest(new { success = false, message = "Part name is required." });
            }

            if (model.UnitCost.HasValue && model.UnitCost.Value < 0)
            {
                return BadRequest(new { success = false, message = "Unit cost cannot be negative." });
            }

            if (model.Quantity < 0)
            {
                return BadRequest(new { success = false, message = "Quantity cannot be negative." });
            }

            // Check for duplicate part number (excluding current part)
            if (!string.IsNullOrWhiteSpace(model.PartNumber))
            {
                var duplicate = await _context.Parts
                    .AnyAsync(p => p.CompanyId == companyId && 
                                  p.PartNumber == model.PartNumber &&
                                  p.PartId != id);

                if (duplicate)
                {
                    return BadRequest(new { success = false, message = "Part number already exists in your inventory." });
                }
            }

            // Update fields
            part.PartName = model.PartName.Trim();
            part.PartNumber = model.PartNumber?.Trim();
            part.Description = model.Description?.Trim();
            part.Quantity = model.Quantity;
            part.UnitCost = model.UnitCost;
            part.ReorderLevel = model.ReorderLevel;
            part.Location = model.Location?.Trim();
            part.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = "Part updated successfully!"
            });
        }

        /// <summary>
        /// Toggle part active status
        /// </summary>
        [HttpPost]
        [Route("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var part = await _context.Parts
                .FirstOrDefaultAsync(p => p.PartId == id && p.CompanyId == companyId);

            if (part == null)
            {
                return NotFound(new { success = false, message = "Part not found." });
            }

            part.IsActive = !part.IsActive;
            part.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = $"Part {(part.IsActive ? "activated" : "deactivated")} successfully!",
                isActive = part.IsActive
            });
        }

        /// <summary>
        /// Adjust part quantity (for manual inventory adjustments)
        /// </summary>
        [HttpPost]
        [Route("{id}/adjust-quantity")]
        public async Task<IActionResult> AdjustQuantity(int id, [FromBody] QuantityAdjustmentModel model)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var part = await _context.Parts
                .FirstOrDefaultAsync(p => p.PartId == id && p.CompanyId == companyId);

            if (part == null)
            {
                return NotFound(new { success = false, message = "Part not found." });
            }

            var newQuantity = part.Quantity + model.AdjustmentAmount;

            if (newQuantity < 0)
            {
                return BadRequest(new { success = false, message = "Adjustment would result in negative quantity." });
            }

            part.Quantity = newQuantity;
            part.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = "Quantity adjusted successfully!",
                newQuantity = part.Quantity
            });
        }
    }

    /// <summary>
    /// Model for quantity adjustment requests
    /// </summary>
    public class QuantityAdjustmentModel
    {
        public int AdjustmentAmount { get; set; } // Can be positive or negative
        public string? Reason { get; set; }
    }
}
