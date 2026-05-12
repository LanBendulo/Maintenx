namespace IT15_Project.Services.Interfaces
{
    /// <summary>
    /// Preventive Maintenance Governance Service
    /// Enforces lifecycle rules for PM work order generation
    /// Prevents duplicate work orders and ensures proper PM cycle management
    /// </summary>
    public interface IPMGovernanceService
    {
        /// <summary>
        /// Check if a PM schedule can generate a work order
        /// Validates: schedule is due AND no active work order exists
        /// </summary>
        /// <param name="scheduleId">PM schedule ID</param>
        /// <returns>Governance result with validation status and reason</returns>
        Task<PMGovernanceResult> CanGenerateWorkOrderAsync(int scheduleId);

        /// <summary>
        /// Check if a PM schedule has an active work order
        /// Active = Pending, Approved, In Progress, Open
        /// </summary>
        /// <param name="scheduleId">PM schedule ID</param>
        /// <returns>True if active work order exists</returns>
        Task<bool> HasActiveWorkOrderAsync(int scheduleId);

        /// <summary>
        /// Check if a PM schedule is currently due or overdue
        /// </summary>
        /// <param name="scheduleId">PM schedule ID</param>
        /// <returns>True if schedule is due</returns>
        Task<bool> IsScheduleDueAsync(int scheduleId);

        /// <summary>
        /// Get detailed generation status for UI display
        /// Includes: due status, active work order info, generation eligibility
        /// </summary>
        /// <param name="scheduleId">PM schedule ID</param>
        /// <returns>Detailed status for UI rendering</returns>
        Task<PMGenerationStatus> GetGenerationStatusAsync(int scheduleId);

        /// <summary>
        /// Get active work order ID for a PM schedule (if exists)
        /// </summary>
        /// <param name="scheduleId">PM schedule ID</param>
        /// <returns>Work order ID or null</returns>
        Task<int?> GetActiveWorkOrderIdAsync(int scheduleId);
    }

    /// <summary>
    /// Result of PM governance validation
    /// </summary>
    public class PMGovernanceResult
    {
        public bool CanGenerate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public PMGovernanceBlockReason? BlockReason { get; set; }
        public int? ExistingWorkOrderId { get; set; }
    }

    /// <summary>
    /// Reasons why PM work order generation is blocked
    /// </summary>
    public enum PMGovernanceBlockReason
    {
        NotDue,
        ActiveWorkOrderExists,
        ScheduleInactive,
        ScheduleNotFound,
        AssetRetired
    }

    /// <summary>
    /// Detailed PM generation status for UI
    /// </summary>
    public class PMGenerationStatus
    {
        public bool IsDue { get; set; }
        public bool IsOverdue { get; set; }
        public bool HasActiveWorkOrder { get; set; }
        public bool CanGenerate { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public string TooltipMessage { get; set; } = string.Empty;
        public int? ActiveWorkOrderId { get; set; }
        public string? ActiveWorkOrderStatus { get; set; }
        public DateTime? NextDueDate { get; set; }
        public int? DaysUntilDue { get; set; }
        public int? DaysOverdue { get; set; }
    }
}
