namespace IT15_Project.Constants
{
    /// <summary>
    /// Centralized Work Order status constants
    /// Ensures consistency across the entire system
    /// CANONICAL STATUSES: Pending → In Progress → Completed/Cancelled
    /// </summary>
    public static class WorkOrderStatuses
    {
        // Core statuses (canonical workflow)
        public const string Pending = "Pending";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        // Future-ready statuses (for Phase 3+)
        public const string OnHold = "On Hold";
        public const string AwaitingParts = "Awaiting Parts";
        public const string AwaitingApproval = "Awaiting Approval";
        public const string Reopened = "Reopened";

        /// <summary>
        /// All valid Work Order statuses
        /// </summary>
        public static readonly string[] All =
        {
            Pending,
            InProgress,
            Completed,
            Cancelled
        };

        /// <summary>
        /// Active statuses (work orders that are not yet completed or cancelled)
        /// </summary>
        public static readonly string[] Active =
        {
            Pending,
            InProgress
        };

        /// <summary>
        /// Terminal statuses (work orders that cannot be modified)
        /// </summary>
        public static readonly string[] Terminal =
        {
            Completed,
            Cancelled
        };

        /// <summary>
        /// Validate if a status string is valid
        /// </summary>
        public static bool IsValid(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return All.Contains(status);
        }

        /// <summary>
        /// Check if a status is terminal (Completed or Cancelled)
        /// </summary>
        public static bool IsTerminal(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return Terminal.Contains(status);
        }

        /// <summary>
        /// Check if a status is active (not Completed or Cancelled)
        /// </summary>
        public static bool IsActive(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return Active.Contains(status);
        }

        /// <summary>
        /// Check if a work order can be started (Pending → In Progress)
        /// </summary>
        public static bool CanStart(string? currentStatus)
        {
            return currentStatus == Pending;
        }

        /// <summary>
        /// Check if a work order can be completed (In Progress → Completed)
        /// </summary>
        public static bool CanComplete(string? currentStatus)
        {
            return currentStatus == InProgress;
        }

        /// <summary>
        /// Check if a work order can be cancelled
        /// </summary>
        public static bool CanCancel(string? currentStatus)
        {
            return currentStatus == Pending || 
                   currentStatus == InProgress;
        }

        /// <summary>
        /// Check if a work order can be edited
        /// </summary>
        public static bool CanEdit(string? currentStatus)
        {
            return !IsTerminal(currentStatus);
        }

        /// <summary>
        /// Check if a work order can be archived
        /// </summary>
        public static bool CanArchive(string? currentStatus)
        {
            return IsTerminal(currentStatus);
        }

        /// <summary>
        /// Get valid transitions from a given status
        /// </summary>
        public static List<string> GetValidTransitions(string? currentStatus)
        {
            return currentStatus switch
            {
                Pending => new List<string> { InProgress, Cancelled },
                InProgress => new List<string> { Completed, Cancelled },
                Completed => new List<string>(), // No transitions from Completed
                Cancelled => new List<string>(), // No transitions from Cancelled
                _ => new List<string>()
            };
        }

        /// <summary>
        /// Validate if a status transition is allowed
        /// </summary>
        public static bool IsValidTransition(string? fromStatus, string? toStatus)
        {
            if (string.IsNullOrWhiteSpace(fromStatus) || string.IsNullOrWhiteSpace(toStatus))
                return false;

            if (fromStatus == toStatus)
                return true; // Same status is always valid

            var validTransitions = GetValidTransitions(fromStatus);
            return validTransitions.Contains(toStatus);
        }

        /// <summary>
        /// Normalize legacy status values to canonical values
        /// Handles case variations and legacy values
        /// </summary>
        public static string? Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            var normalized = status.Trim();

            // Handle case-insensitive matching and legacy values
            return normalized.ToLowerInvariant() switch
            {
                "open" => Pending,  // LEGACY: "Open" maps to "Pending"
                "pending" => Pending,
                "in progress" => InProgress,
                "inprogress" => InProgress,
                "in-progress" => InProgress,
                "completed" => Completed,
                "complete" => Completed,
                "done" => Completed,
                "cancelled" => Cancelled,
                "canceled" => Cancelled,
                "on hold" => OnHold,
                "onhold" => OnHold,
                "awaiting parts" => AwaitingParts,
                "awaiting approval" => AwaitingApproval,
                "reopened" => Reopened,
                _ => normalized // Return original if no match
            };
        }
    }
}
