using IT15_Project.Constants;
using IT15_Project.Data;
using IT15_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_Project.Services
{
    /// <summary>
    /// Lightweight Preventive Maintenance Work Order Generation Service
    /// Automatically generates work orders from active PM schedules that are due
    /// Executes on application startup and when PM page loads
    /// </summary>
    public class PreventiveMaintenanceGenerationService
    {
        private readonly ApplicationDbContext _context;
        private readonly AssetStatusService _assetStatusService;
        private readonly ILogger<PreventiveMaintenanceGenerationService> _logger;

        // In-memory throttle cache to prevent excessive execution
        private static DateTime? _lastExecutionTime;
        private static readonly TimeSpan ThrottleInterval = TimeSpan.FromMinutes(5);
        private static readonly object _executionLock = new object();

        public PreventiveMaintenanceGenerationService(
            ApplicationDbContext context,
            AssetStatusService assetStatusService,
            ILogger<PreventiveMaintenanceGenerationService> logger)
        {
            _context = context;
            _assetStatusService = assetStatusService;
            _logger = logger;
        }

        /// <summary>
        /// Main entry point for PM work order generation
        /// Throttled to prevent excessive execution
        /// </summary>
        public async Task<GenerationResult> GenerateDueWorkOrdersAsync(int? companyId = null)
        {
            // Throttle check - skip if executed recently
            lock (_executionLock)
            {
                if (_lastExecutionTime.HasValue && 
                    DateTime.Now - _lastExecutionTime.Value < ThrottleInterval)
                {
                    _logger.LogDebug(
                        "PM generation skipped - last execution was {Seconds} seconds ago (throttle: {ThrottleMinutes} minutes)",
                        (DateTime.Now - _lastExecutionTime.Value).TotalSeconds,
                        ThrottleInterval.TotalMinutes
                    );
                    return new GenerationResult { Skipped = true, Reason = "Throttled" };
                }

                _lastExecutionTime = DateTime.Now;
            }

            var result = new GenerationResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("Starting PM work order generation scan...");

                // Get all active PM schedules that are due
                var dueSchedules = await GetDueSchedulesAsync(companyId);

                _logger.LogInformation("Found {Count} due PM schedules", dueSchedules.Count);

                foreach (var schedule in dueSchedules)
                {
                    try
                    {
                        var generated = await TryGenerateWorkOrderAsync(schedule);
                        
                        if (generated)
                        {
                            result.SuccessCount++;
                            result.GeneratedWorkOrderIds.Add(schedule.LastGeneratedWorkOrderId!.Value);
                        }
                        else
                        {
                            result.SkippedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Schedule {schedule.ScheduleId}: {ex.Message}");
                        
                        _logger.LogError(ex, 
                            "Failed to generate work order for PM schedule {ScheduleId} (Asset: {AssetId})",
                            schedule.ScheduleId,
                            schedule.AssetId
                        );

                        // Record error in schedule
                        schedule.LastGenerationAttempt = DateTime.Now;
                        schedule.LastGenerationError = ex.Message.Length > 500 
                            ? ex.Message.Substring(0, 497) + "..." 
                            : ex.Message;
                        
                        await _context.SaveChangesAsync();
                    }
                }

                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;

                _logger.LogInformation(
                    "PM generation completed: {Success} generated, {Skipped} skipped, {Failed} failed in {Duration}ms",
                    result.SuccessCount,
                    result.SkippedCount,
                    result.FailureCount,
                    result.Duration.TotalMilliseconds
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during PM generation scan");
                result.Errors.Add($"Critical error: {ex.Message}");
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                return result;
            }
        }

        /// <summary>
        /// Get all PM schedules that are due for work order generation
        /// </summary>
        private async Task<List<PreventiveSchedule>> GetDueSchedulesAsync(int? companyId)
        {
            var today = DateTime.Today;

            var query = _context.PreventiveSchedules
                .Include(ps => ps.Asset)
                .Include(ps => ps.DefaultTechnician)
                .Where(ps => ps.IsActive && ps.NextDueDate <= today);

            // Filter by company if specified (for tenant-specific execution)
            if (companyId.HasValue)
            {
                query = query.Where(ps => ps.CompanyId == companyId.Value);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Attempt to generate a work order for a PM schedule
        /// Returns true if generated, false if skipped (duplicate prevention)
        /// </summary>
        private async Task<bool> TryGenerateWorkOrderAsync(PreventiveSchedule schedule)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // DUPLICATE PREVENTION: Check if already generated for this cycle
                if (schedule.LastGeneratedDate.HasValue && 
                    schedule.LastGeneratedDate.Value >= schedule.NextDueDate)
                {
                    _logger.LogDebug(
                        "Skipping PM schedule {ScheduleId} - already generated for current cycle (LastGen: {LastGen}, NextDue: {NextDue})",
                        schedule.ScheduleId,
                        schedule.LastGeneratedDate.Value.ToShortDateString(),
                        schedule.NextDueDate.ToShortDateString()
                    );
                    return false;
                }

                // ASSET VALIDATION: Check if asset is retired
                if (schedule.Asset == null)
                {
                    _logger.LogWarning(
                        "Skipping PM schedule {ScheduleId} - asset {AssetId} not found",
                        schedule.ScheduleId,
                        schedule.AssetId
                    );
                    return false;
                }

                if (schedule.Asset.Status == AssetStatuses.Retired)
                {
                    _logger.LogInformation(
                        "Skipping PM schedule {ScheduleId} - asset {AssetId} is retired",
                        schedule.ScheduleId,
                        schedule.AssetId
                    );
                    return false;
                }

                // DUPLICATE PREVENTION: Check for existing active PM work orders
                var hasActiveWorkOrder = await _context.WorkOrders
                    .AnyAsync(wo => wo.CompanyId == schedule.CompanyId &&
                                   wo.AssetId == schedule.AssetId &&
                                   wo.Source == "Preventive" &&
                                   (wo.Status == WorkOrderStatuses.Pending || 
                                    wo.Status == WorkOrderStatuses.InProgress));

                if (hasActiveWorkOrder)
                {
                    _logger.LogDebug(
                        "Skipping PM schedule {ScheduleId} - active PM work order already exists for asset {AssetId}",
                        schedule.ScheduleId,
                        schedule.AssetId
                    );
                    return false;
                }

                // CREATE WORK ORDER
                var workOrder = new WorkOrder
                {
                    CompanyId = schedule.CompanyId,
                    AssetId = schedule.AssetId,
                    AssignedTo = schedule.DefaultTechnicianId,
                    CreatedBy = null, // System-generated (no user context)
                    Status = WorkOrderStatuses.Pending,
                    Priority = schedule.Priority ?? "Medium",
                    Description = $"Preventive Maintenance: {schedule.Title}\n\n{schedule.Description ?? ""}".Trim(),
                    DateCreated = DateTime.Now,
                    DueDate = schedule.NextDueDate,
                    Source = "Preventive"
                };

                _context.WorkOrders.Add(workOrder);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Generated PM work order {WorkOrderId} for schedule {ScheduleId} (Asset: {AssetName})",
                    workOrder.WorkOrderId,
                    schedule.ScheduleId,
                    schedule.Asset.AssetName
                );

                // UPDATE SCHEDULE: Record generation and calculate next due date
                schedule.LastGeneratedDate = DateTime.Today;
                schedule.LastGeneratedWorkOrderId = workOrder.WorkOrderId;
                schedule.LastGenerationAttempt = DateTime.Now;
                schedule.LastGenerationError = null; // Clear previous errors
                schedule.NextDueDate = CalculateNextDueDate(schedule.NextDueDate, schedule.FrequencyDays);
                schedule.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated PM schedule {ScheduleId} - next due date: {NextDueDate}",
                    schedule.ScheduleId,
                    schedule.NextDueDate.ToShortDateString()
                );

                // ASSET STATUS UPDATE: Mark asset as Under Maintenance (if not already)
                if (schedule.Asset.Status == AssetStatuses.Active)
                {
                    await _assetStatusService.OnWorkOrderCreatedAsync(workOrder.WorkOrderId, null);
                    _logger.LogDebug(
                        "Asset {AssetId} status updated to Under Maintenance",
                        schedule.AssetId
                    );
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Calculate next due date based on frequency
        /// </summary>
        private DateTime CalculateNextDueDate(DateTime currentDueDate, int frequencyDays)
        {
            // Calculate from current due date (not today) to maintain schedule consistency
            return currentDueDate.AddDays(frequencyDays);
        }

        /// <summary>
        /// Get generation statistics for monitoring
        /// </summary>
        public async Task<GenerationStatistics> GetStatisticsAsync(int? companyId = null)
        {
            var query = _context.PreventiveSchedules.AsQueryable();

            if (companyId.HasValue)
            {
                query = query.Where(ps => ps.CompanyId == companyId.Value);
            }

            var stats = new GenerationStatistics
            {
                TotalActiveSchedules = await query.CountAsync(ps => ps.IsActive),
                DueSchedules = await query.CountAsync(ps => ps.IsActive && ps.NextDueDate <= DateTime.Today),
                OverdueSchedules = await query.CountAsync(ps => ps.IsActive && ps.NextDueDate < DateTime.Today),
                SchedulesWithErrors = await query.CountAsync(ps => ps.IsActive && ps.LastGenerationError != null),
                LastExecutionTime = _lastExecutionTime
            };

            return stats;
        }
    }

    /// <summary>
    /// Result of a PM generation execution
    /// </summary>
    public class GenerationResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int SuccessCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailureCount { get; set; }
        public bool Skipped { get; set; }
        public string? Reason { get; set; }
        public List<int> GeneratedWorkOrderIds { get; set; } = new();
        public List<string> Errors { get; set; } = new();

        public bool HasErrors => Errors.Count > 0;
        public int TotalProcessed => SuccessCount + SkippedCount + FailureCount;
    }

    /// <summary>
    /// Statistics about PM generation system
    /// </summary>
    public class GenerationStatistics
    {
        public int TotalActiveSchedules { get; set; }
        public int DueSchedules { get; set; }
        public int OverdueSchedules { get; set; }
        public int SchedulesWithErrors { get; set; }
        public DateTime? LastExecutionTime { get; set; }
    }
}
