using IT15_Project.Constants;
using IT15_Project.Data;
using IT15_Project.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IT15_Project.Services
{
    /// <summary>
    /// Preventive Maintenance Governance Service Implementation
    /// Enforces CMMS lifecycle rules for PM work order generation
    /// Prevents duplicate work orders and ensures proper maintenance cycle management
    /// MULTI-TENANT: All queries filtered by CompanyId
    /// </summary>
    public class PMGovernanceService : IPMGovernanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<PMGovernanceService> _logger;

        public PMGovernanceService(
            ApplicationDbContext context,
            ITenantService tenantService,
            ILogger<PMGovernanceService> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        /// <summary>
        /// CORE GOVERNANCE VALIDATION
        /// Checks if PM schedule can generate a work order
        /// Business Rules:
        /// 1. Schedule must be active
        /// 2. Schedule must be due or overdue
        /// 3. No active work order must exist for this schedule
        /// 4. Asset must not be retired
        /// </summary>
        public async Task<PMGovernanceResult> CanGenerateWorkOrderAsync(int scheduleId)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Get schedule with asset
            var schedule = await _context.PreventiveSchedules
                .Include(ps => ps.Asset)
                .FirstOrDefaultAsync(ps => ps.ScheduleId == scheduleId && ps.CompanyId == companyId);

            if (schedule == null)
            {
                return new PMGovernanceResult
                {
                    CanGenerate = false,
                    Reason = "Preventive maintenance schedule not found.",
                    BlockReason = PMGovernanceBlockReason.ScheduleNotFound
                };
            }

            // Check if schedule is active
            if (!schedule.IsActive)
            {
                return new PMGovernanceResult
                {
                    CanGenerate = false,
                    Reason = "Cannot generate work order from inactive schedule.",
                    BlockReason = PMGovernanceBlockReason.ScheduleInactive
                };
            }

            // Check if asset is retired
            if (schedule.Asset?.Status == AssetStatuses.Retired)
            {
                return new PMGovernanceResult
                {
                    CanGenerate = false,
                    Reason = "Cannot generate work order for retired asset.",
                    BlockReason = PMGovernanceBlockReason.AssetRetired
                };
            }

            // Check if schedule is due
            var isDue = schedule.NextDueDate <= DateTime.Today;
            if (!isDue)
            {
                var daysUntilDue = (schedule.NextDueDate - DateTime.Today).Days;
                return new PMGovernanceResult
                {
                    CanGenerate = false,
                    Reason = $"Schedule is not due yet. Next due date: {schedule.NextDueDate:yyyy-MM-dd} ({daysUntilDue} days).",
                    BlockReason = PMGovernanceBlockReason.NotDue
                };
            }

            // Check for active work orders linked to this schedule
            var activeWorkOrder = await _context.WorkOrders
                .Where(wo => wo.PreventiveScheduleId == scheduleId &&
                            wo.CompanyId == companyId &&
                            !wo.IsArchived &&
                            (wo.Status == WorkOrderStatuses.Pending ||
                             wo.Status == WorkOrderStatuses.InProgress ||
                             wo.Status == "Open")) // Support legacy "Open" status
                .Select(wo => new { wo.WorkOrderId, wo.Status })
                .FirstOrDefaultAsync();

            if (activeWorkOrder != null)
            {
                return new PMGovernanceResult
                {
                    CanGenerate = false,
                    Reason = $"An active work order (#{activeWorkOrder.WorkOrderId}) already exists for this maintenance schedule.",
                    BlockReason = PMGovernanceBlockReason.ActiveWorkOrderExists,
                    ExistingWorkOrderId = activeWorkOrder.WorkOrderId
                };
            }

            // All checks passed - generation allowed
            _logger.LogDebug(
                "PM schedule {ScheduleId} passed governance validation - generation allowed",
                scheduleId
            );

            return new PMGovernanceResult
            {
                CanGenerate = true,
                Reason = "Schedule is due and no active work order exists."
            };
        }

        /// <summary>
        /// Check if PM schedule has an active work order
        /// Active statuses: Pending, In Progress, Open (legacy)
        /// </summary>
        public async Task<bool> HasActiveWorkOrderAsync(int scheduleId)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            return await _context.WorkOrders
                .AnyAsync(wo => wo.PreventiveScheduleId == scheduleId &&
                               wo.CompanyId == companyId &&
                               !wo.IsArchived &&
                               (wo.Status == WorkOrderStatuses.Pending ||
                                wo.Status == WorkOrderStatuses.InProgress ||
                                wo.Status == "Open"));
        }

        /// <summary>
        /// Check if PM schedule is currently due or overdue
        /// </summary>
        public async Task<bool> IsScheduleDueAsync(int scheduleId)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var schedule = await _context.PreventiveSchedules
                .FirstOrDefaultAsync(ps => ps.ScheduleId == scheduleId && ps.CompanyId == companyId);

            if (schedule == null)
                return false;

            return schedule.IsActive && schedule.NextDueDate <= DateTime.Today;
        }

        /// <summary>
        /// Get detailed generation status for UI display
        /// Provides all information needed for button state, tooltips, and visual indicators
        /// </summary>
        public async Task<PMGenerationStatus> GetGenerationStatusAsync(int scheduleId)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            var schedule = await _context.PreventiveSchedules
                .Include(ps => ps.Asset)
                .FirstOrDefaultAsync(ps => ps.ScheduleId == scheduleId && ps.CompanyId == companyId);

            if (schedule == null)
            {
                return new PMGenerationStatus
                {
                    CanGenerate = false,
                    StatusMessage = "Not Found",
                    TooltipMessage = "Schedule not found"
                };
            }

            var today = DateTime.Today;
            var isDue = schedule.NextDueDate <= today;
            var isOverdue = schedule.NextDueDate < today;
            var daysUntilDue = (schedule.NextDueDate - today).Days;
            var daysOverdue = isDue ? (today - schedule.NextDueDate).Days : 0;

            // Check for active work order
            var activeWorkOrder = await _context.WorkOrders
                .Where(wo => wo.PreventiveScheduleId == scheduleId &&
                            wo.CompanyId == companyId &&
                            !wo.IsArchived &&
                            (wo.Status == WorkOrderStatuses.Pending ||
                             wo.Status == WorkOrderStatuses.InProgress ||
                             wo.Status == "Open"))
                .Select(wo => new { wo.WorkOrderId, wo.Status })
                .FirstOrDefaultAsync();

            var hasActiveWorkOrder = activeWorkOrder != null;

            // Determine if generation is allowed
            var canGenerate = schedule.IsActive && 
                             isDue && 
                             !hasActiveWorkOrder && 
                             schedule.Asset?.Status != AssetStatuses.Retired;

            // Build status message
            string statusMessage;
            string tooltipMessage;

            if (!schedule.IsActive)
            {
                statusMessage = "Inactive";
                tooltipMessage = "Schedule is inactive";
            }
            else if (schedule.Asset?.Status == AssetStatuses.Retired)
            {
                statusMessage = "Asset Retired";
                tooltipMessage = "Cannot generate - asset is retired";
            }
            else if (hasActiveWorkOrder)
            {
                statusMessage = "Active WO";
                tooltipMessage = $"Work order #{activeWorkOrder!.WorkOrderId} is {activeWorkOrder.Status}";
            }
            else if (isOverdue)
            {
                statusMessage = $"Overdue ({daysOverdue}d)";
                tooltipMessage = $"Overdue by {daysOverdue} day(s) - ready to generate";
            }
            else if (isDue)
            {
                statusMessage = "Due Today";
                tooltipMessage = "Due today - ready to generate";
            }
            else
            {
                statusMessage = $"Due in {daysUntilDue}d";
                tooltipMessage = $"Not due yet - next due: {schedule.NextDueDate:yyyy-MM-dd}";
            }

            return new PMGenerationStatus
            {
                IsDue = isDue,
                IsOverdue = isOverdue,
                HasActiveWorkOrder = hasActiveWorkOrder,
                CanGenerate = canGenerate,
                StatusMessage = statusMessage,
                TooltipMessage = tooltipMessage,
                ActiveWorkOrderId = activeWorkOrder?.WorkOrderId,
                ActiveWorkOrderStatus = activeWorkOrder?.Status,
                NextDueDate = schedule.NextDueDate,
                DaysUntilDue = daysUntilDue > 0 ? daysUntilDue : null,
                DaysOverdue = daysOverdue > 0 ? daysOverdue : null
            };
        }

        /// <summary>
        /// Get active work order ID for a PM schedule
        /// Returns null if no active work order exists
        /// </summary>
        public async Task<int?> GetActiveWorkOrderIdAsync(int scheduleId)
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            return await _context.WorkOrders
                .Where(wo => wo.PreventiveScheduleId == scheduleId &&
                            wo.CompanyId == companyId &&
                            !wo.IsArchived &&
                            (wo.Status == WorkOrderStatuses.Pending ||
                             wo.Status == WorkOrderStatuses.InProgress ||
                             wo.Status == "Open"))
                .Select(wo => wo.WorkOrderId)
                .FirstOrDefaultAsync();
        }
    }
}
