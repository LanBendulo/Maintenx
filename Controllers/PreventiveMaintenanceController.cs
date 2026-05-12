using IT15_Project.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Services;
using IT15_Project.Services.Interfaces;
using System.Security.Claims;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// Preventive Maintenance Scheduling for Owner/Admin roles only.
    /// Manages schedules that generate work orders for planned maintenance.
    /// MULTI-TENANT: All queries filtered by CompanyId
    /// </summary>
    [Authorize(Roles = "Owner,Admin,Supervisor")]
    [Route("admin/preventive-maintenance")]
    public class PreventiveMaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly PreventiveMaintenanceGenerationService _generationService;
        private readonly IPMGovernanceService _governanceService;

        public PreventiveMaintenanceController(
            ApplicationDbContext context, 
            ITenantService tenantService,
            PreventiveMaintenanceGenerationService generationService,
            IPMGovernanceService governanceService)
        {
            _context = context;
            _tenantService = tenantService;
            _generationService = generationService;
            _governanceService = governanceService;
        }

        /// <summary>
        /// List all preventive maintenance schedules
        /// Triggers automatic PM work order generation on page load
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index(string filter = "active")
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // ─── AUTOMATIC PM GENERATION ───────────────────
            // Execute PM generation for current company (throttled to prevent excessive execution)
            try
            {
                var generationResult = await _generationService.GenerateDueWorkOrdersAsync(companyId);
                
                if (!generationResult.Skipped && generationResult.SuccessCount > 0)
                {
                    TempData["SuccessMessage"] = $"Automatically generated {generationResult.SuccessCount} PM work order(s)";
                }

                if (generationResult.HasErrors)
                {
                    TempData["WarningMessage"] = $"PM generation completed with {generationResult.FailureCount} error(s)";
                }
            }
            catch (Exception ex)
            {
                // Log but don't block page load
                Console.WriteLine($"[PM Generation Error] {ex.Message}");
            }
            // ───────────────────────────────────────────────

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
                    .Where(a => a.CompanyId == companyId && a.Status != AssetStatuses.Retired)
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

                // Validate next due date is not in the past
                if (request.NextDueDate.Date < DateTime.Today)
                {
                    return BadRequest(new { success = false, message = "Next due date cannot be in the past." });
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
                    Priority = request.Priority ?? "Medium",
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
                    defaultTechnicianName = schedule.DefaultTechnician?.FullName,
                    priority = schedule.Priority
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
                {
                    // Validate next due date is not in the past
                    if (request.NextDueDate.Value.Date < DateTime.Today)
                    {
                        return BadRequest(new { success = false, message = "Next due date cannot be in the past." });
                    }
                    schedule.NextDueDate = request.NextDueDate.Value;
                }

                schedule.DefaultTechnicianId = request.DefaultTechnicianId;
                
                if (!string.IsNullOrEmpty(request.Priority))
                    schedule.Priority = request.Priority;
                
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
        /// Check if a PM schedule can generate a work order (governance validation)
        /// Used by UI to enable/disable generation button and show tooltips
        /// </summary>
        [HttpGet]
        [Route("{id}/can-generate")]
        public async Task<IActionResult> CanGenerate(int id)
        {
            try
            {
                var status = await _governanceService.GetGenerationStatusAsync(id);
                
                return Ok(new
                {
                    canGenerate = status.CanGenerate,
                    isDue = status.IsDue,
                    isOverdue = status.IsOverdue,
                    hasActiveWorkOrder = status.HasActiveWorkOrder,
                    statusMessage = status.StatusMessage,
                    tooltipMessage = status.TooltipMessage,
                    activeWorkOrderId = status.ActiveWorkOrderId,
                    activeWorkOrderStatus = status.ActiveWorkOrderStatus,
                    nextDueDate = status.NextDueDate,
                    daysUntilDue = status.DaysUntilDue,
                    daysOverdue = status.DaysOverdue
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate a work order from a preventive maintenance schedule
        /// GOVERNANCE ENFORCED: Validates schedule is due and no active work order exists
        /// </summary>
        [HttpPost]
        [Route("{id}/generate")]
        public async Task<IActionResult> GenerateWorkOrder(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // ═══════════════════════════════════════════════════════════
                // GOVERNANCE VALIDATION (CRITICAL)
                // ═══════════════════════════════════════════════════════════
                var governanceResult = await _governanceService.CanGenerateWorkOrderAsync(id);
                
                if (!governanceResult.CanGenerate)
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = governanceResult.Reason,
                        blockReason = governanceResult.BlockReason?.ToString(),
                        existingWorkOrderId = governanceResult.ExistingWorkOrderId
                    });
                }
                // ═══════════════════════════════════════════════════════════

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

                // Create work order with PreventiveScheduleId link
                // Calculate expected completion based on priority
                var completionBuffer = schedule.Priority?.ToLower() switch
                {
                    "high" => 2,      // High priority: 2 days
                    "low" => 7,       // Low priority: 7 days
                    _ => 5            // Medium/default: 5 days
                };
                
                var workOrder = new WorkOrder
                {
                    CompanyId = companyId,
                    AssetId = schedule.AssetId,
                    AssignedTo = schedule.DefaultTechnicianId,
                    CreatedBy = currentPersonnel.PersonnelId,
                    Status = WorkOrderStatuses.Pending,
                    Priority = schedule.Priority ?? "Medium",
                    Description = $"{schedule.Title}\n\n{schedule.Description ?? "Preventive Maintenance"}",
                    DateCreated = DateTime.Now,
                    DueDate = DateTime.Today.AddDays(completionBuffer), // Smart completion date based on priority
                    Source = "Preventive",
                    PreventiveScheduleId = schedule.ScheduleId  // ← GOVERNANCE: Link to PM schedule
                };

                _context.WorkOrders.Add(workOrder);

                // Update schedule tracking
                schedule.LastGeneratedDate = DateTime.Today;
                schedule.LastGeneratedWorkOrderId = workOrder.WorkOrderId;
                schedule.LastGenerationAttempt = DateTime.Now;
                schedule.LastGenerationError = null; // Clear any previous errors
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
        public string Priority { get; set; } = "Medium";
    }

    public class EditScheduleRequest
    {
        public int? AssetId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? FrequencyDays { get; set; }
        public DateTime? NextDueDate { get; set; }
        public int? DefaultTechnicianId { get; set; }
        public string? Priority { get; set; }
    }
}
