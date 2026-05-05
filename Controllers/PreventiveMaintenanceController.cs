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
    /// Preventive Maintenance Scheduling for Owner/Admin roles only.
    /// Manages schedules that generate work orders for planned maintenance.
    /// MULTI-TENANT: All queries filtered by CompanyId
    /// </summary>
    [Authorize(Roles = "Owner,Admin")]
    [Route("admin/preventive-maintenance")]
    public class PreventiveMaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public PreventiveMaintenanceController(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        /// <summary>
        /// List all preventive maintenance schedules
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index(string filter = "active")
        {
            var companyId = _tenantService.GetCurrentCompanyId();

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
            ViewData["Active"] = "PreventiveMaintenance";
            return View("PreventiveMaintenance", schedules);
        }

        /// <summary>
        /// Get assets for dropdown (tenant-filtered)
        /// </summary>
        [HttpGet]
        [Route("assets/list")]
        public async Task<IActionResult> GetAssets()
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var assets = await _context.Assets
                    .Where(a => a.CompanyId == companyId && a.Status != "Retired")
                    .Select(a => new { value = a.AssetId, text = a.AssetName })
                    .ToListAsync();

                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load assets", error = ex.Message });
            }
        }

        /// <summary>
        /// Get technicians for dropdown (tenant-filtered)
        /// </summary>
        [HttpGet]
        [Route("technicians/list")]
        public async Task<IActionResult> GetTechnicians()
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var technicians = await _context.Personnel
                    .Where(p => p.CompanyId == companyId && 
                               p.IsActive && 
                               p.Role == "Technician")
                    .Select(p => new { value = p.PersonnelId, text = p.FullName })
                    .ToListAsync();

                return Ok(technicians);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to load technicians", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new preventive maintenance schedule
        /// </summary>
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] CreateScheduleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                // Validate asset belongs to company
                var asset = await _context.Assets
                    .FirstOrDefaultAsync(a => a.AssetId == request.AssetId && a.CompanyId == companyId);

                if (asset == null)
                {
                    return BadRequest(new { success = false, message = "Asset not found or does not belong to your company." });
                }

                // Validate technician if provided
                if (request.DefaultTechnicianId.HasValue)
                {
                    var technician = await _context.Personnel
                        .FirstOrDefaultAsync(p => p.PersonnelId == request.DefaultTechnicianId.Value && 
                                                 p.CompanyId == companyId && 
                                                 p.Role == "Technician");

                    if (technician == null)
                    {
                        return BadRequest(new { success = false, message = "Technician not found or does not belong to your company." });
                    }
                }

                // Validate frequency
                if (request.FrequencyDays <= 0)
                {
                    return BadRequest(new { success = false, message = "Frequency must be greater than 0 days." });
                }

                var schedule = new PreventiveSchedule
                {
                    CompanyId = companyId,
                    AssetId = request.AssetId,
                    Title = request.Title,
                    Description = request.Description,
                    FrequencyDays = request.FrequencyDays,
                    NextDueDate = request.NextDueDate,
                    IsActive = true,
                    DefaultTechnicianId = request.DefaultTechnicianId,
                    CreatedAt = DateTime.Now
                };

                _context.PreventiveSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, scheduleId = schedule.ScheduleId, message = "Preventive maintenance schedule created successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while creating the schedule.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get schedule details
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetSchedule(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var schedule = await _context.PreventiveSchedules
                    .Where(ps => ps.ScheduleId == id && ps.CompanyId == companyId)
                    .Include(ps => ps.Asset)
                    .Include(ps => ps.DefaultTechnician)
                    .FirstOrDefaultAsync();

                if (schedule == null)
                {
                    return NotFound(new { success = false, message = "Schedule not found." });
                }

                var result = new
                {
                    scheduleId = schedule.ScheduleId,
                    assetId = schedule.AssetId,
                    assetName = schedule.Asset?.AssetName,
                    title = schedule.Title,
                    description = schedule.Description,
                    frequencyDays = schedule.FrequencyDays,
                    nextDueDate = schedule.NextDueDate,
                    lastCompletedDate = schedule.LastCompletedDate,
                    isActive = schedule.IsActive,
                    defaultTechnicianId = schedule.DefaultTechnicianId,
                    defaultTechnicianName = schedule.DefaultTechnician?.FullName
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing schedule
        /// </summary>
        [HttpPut]
        [Route("{id}/edit")]
        public async Task<IActionResult> Edit(int id, [FromBody] EditScheduleRequest request)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var schedule = await _context.PreventiveSchedules
                    .FirstOrDefaultAsync(ps => ps.ScheduleId == id && ps.CompanyId == companyId);

                if (schedule == null)
                {
                    return NotFound(new { success = false, message = "Schedule not found." });
                }

                // Validate asset if changed
                if (request.AssetId.HasValue && request.AssetId != schedule.AssetId)
                {
                    var asset = await _context.Assets
                        .FirstOrDefaultAsync(a => a.AssetId == request.AssetId.Value && a.CompanyId == companyId);

                    if (asset == null)
                    {
                        return BadRequest(new { success = false, message = "Asset not found or does not belong to your company." });
                    }

                    schedule.AssetId = request.AssetId.Value;
                }

                // Validate technician if changed
                if (request.DefaultTechnicianId.HasValue)
                {
                    var technician = await _context.Personnel
                        .FirstOrDefaultAsync(p => p.PersonnelId == request.DefaultTechnicianId.Value && 
                                                 p.CompanyId == companyId && 
                                                 p.Role == "Technician");

                    if (technician == null)
                    {
                        return BadRequest(new { success = false, message = "Technician not found or does not belong to your company." });
                    }
                }

                // Update fields
                if (!string.IsNullOrEmpty(request.Title))
                    schedule.Title = request.Title;

                schedule.Description = request.Description;

                if (request.FrequencyDays.HasValue)
                {
                    if (request.FrequencyDays.Value <= 0)
                    {
                        return BadRequest(new { success = false, message = "Frequency must be greater than 0 days." });
                    }
                    schedule.FrequencyDays = request.FrequencyDays.Value;
                }

                if (request.NextDueDate.HasValue)
                    schedule.NextDueDate = request.NextDueDate.Value;

                schedule.DefaultTechnicianId = request.DefaultTechnicianId;
                schedule.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Schedule updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while updating the schedule.", error = ex.Message });
            }
        }

        /// <summary>
        /// Toggle schedule active status
        /// </summary>
        [HttpPut]
        [Route("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var schedule = await _context.PreventiveSchedules
                    .FirstOrDefaultAsync(ps => ps.ScheduleId == id && ps.CompanyId == companyId);

                if (schedule == null)
                {
                    return NotFound(new { success = false, message = "Schedule not found." });
                }

                schedule.IsActive = !schedule.IsActive;
                schedule.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, isActive = schedule.IsActive, message = $"Schedule {(schedule.IsActive ? "activated" : "deactivated")} successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a schedule
        /// </summary>
        [HttpDelete]
        [Route("{id}/delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var schedule = await _context.PreventiveSchedules
                    .FirstOrDefaultAsync(ps => ps.ScheduleId == id && ps.CompanyId == companyId);

                if (schedule == null)
                {
                    return NotFound(new { success = false, message = "Schedule not found." });
                }

                _context.PreventiveSchedules.Remove(schedule);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Schedule deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the schedule.", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate a work order from a preventive maintenance schedule
        /// </summary>
        [HttpPost]
        [Route("{id}/generate")]
        public async Task<IActionResult> GenerateWorkOrder(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Get current user's personnel record
                var currentPersonnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.UserId == userId);

                if (currentPersonnel == null)
                {
                    return BadRequest(new { success = false, message = "Current user does not have a personnel record." });
                }

                // Get schedule
                var schedule = await _context.PreventiveSchedules
                    .Where(ps => ps.ScheduleId == id && ps.CompanyId == companyId)
                    .Include(ps => ps.Asset)
                    .Include(ps => ps.DefaultTechnician)
                    .FirstOrDefaultAsync();

                if (schedule == null)
                {
                    return NotFound(new { success = false, message = "Schedule not found." });
                }

                if (!schedule.IsActive)
                {
                    return BadRequest(new { success = false, message = "Cannot generate work order from inactive schedule." });
                }

                // Create work order
                var workOrder = new WorkOrder
                {
                    CompanyId = companyId,
                    AssetId = schedule.AssetId,
                    AssignedTo = schedule.DefaultTechnicianId,
                    CreatedBy = currentPersonnel.PersonnelId,
                    Status = "Open",
                    Priority = "Medium",
                    Description = $"{schedule.Title}\n\n{schedule.Description ?? "Preventive Maintenance"}",
                    DateCreated = DateTime.Now,
                    DueDate = schedule.NextDueDate,
                    Source = "Preventive"
                };

                _context.WorkOrders.Add(workOrder);

                // Update schedule
                schedule.LastCompletedDate = DateTime.Today;
                schedule.NextDueDate = DateTime.Today.AddDays(schedule.FrequencyDays);
                schedule.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    workOrderId = workOrder.WorkOrderId,
                    nextDueDate = schedule.NextDueDate,
                    message = "Work order generated successfully!" 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while generating the work order.", error = ex.Message });
            }
        }
    }

    // Request models
    public class CreateScheduleRequest
    {
        public int AssetId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int FrequencyDays { get; set; }
        public DateTime NextDueDate { get; set; }
        public int? DefaultTechnicianId { get; set; }
    }

    public class EditScheduleRequest
    {
        public int? AssetId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? FrequencyDays { get; set; }
        public DateTime? NextDueDate { get; set; }
        public int? DefaultTechnicianId { get; set; }
    }
}
