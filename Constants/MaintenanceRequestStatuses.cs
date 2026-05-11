namespace IT15_Project.Constants
{
    /// <summary>
    /// Centralized Maintenance Request Status Constants
    /// Defines canonical lifecycle statuses for maintenance request workflow
    /// </summary>
    public static class MaintenanceRequestStatuses
    {
        // ========================================
        // CANONICAL STATUSES
        // ========================================
        
        /// <summary>Newly submitted request awaiting review</summary>
        public const string Pending = "Pending";
        
        /// <summary>Approved for action, eligible for Work Order conversion</summary>
        public const string Approved = "Approved";
        
        /// <summary>Denied request (terminal state)</summary>
        public const string Rejected = "Rejected";
        
        /// <summary>Successfully converted into Work Order (terminal state)</summary>
        public const string Converted = "Converted";
        
        /// <summary>Manually closed without conversion (terminal state)</summary>
        public const string Closed = "Closed";

        // ========================================
        // STATUS ARRAYS
        // ========================================
        
        /// <summary>Active statuses that can be transitioned</summary>
        public static readonly string[] Active = { Pending, Approved };
        
        /// <summary>Terminal statuses that cannot be changed</summary>
        public static readonly string[] Terminal = { Rejected, Converted, Closed };

        // ========================================
        // VALIDATION HELPERS
        // ========================================
        
        /// <summary>Check if status is valid</summary>
        public static bool IsValid(string? status)
        {
            if (string.IsNullOrEmpty(status)) return false;
            
            return status == Pending || 
                   status == Approved || 
                   status == Rejected || 
                   status == Converted || 
                   status == Closed;
        }

        /// <summary>Check if status is terminal (cannot be changed)</summary>
        public static bool IsTerminal(string? status)
        {
            if (string.IsNullOrEmpty(status)) return false;
            return status == Rejected || status == Converted || status == Closed;
        }

        /// <summary>Check if status is active (can be transitioned)</summary>
        public static bool IsActive(string? status)
        {
            if (string.IsNullOrEmpty(status)) return false;
            return status == Pending || status == Approved;
        }

        /// <summary>Check if request can be approved</summary>
        public static bool CanApprove(string? status)
        {
            return status == Pending;
        }

        /// <summary>Check if request can be rejected</summary>
        public static bool CanReject(string? status)
        {
            return status == Pending;
        }

        /// <summary>Check if request can be converted to Work Order</summary>
        public static bool CanConvert(string? status)
        {
            return status == Approved;
        }

        /// <summary>Check if request can be closed</summary>
        public static bool CanClose(string? status)
        {
            return status == Pending || status == Approved;
        }

        /// <summary>Check if request can be archived</summary>
        public static bool CanArchive(string? status)
        {
            return IsTerminal(status);
        }

        /// <summary>Get valid transitions from current status</summary>
        public static string[] GetValidTransitions(string? currentStatus)
        {
            return currentStatus switch
            {
                Pending => new[] { Approved, Rejected, Closed },
                Approved => new[] { Converted, Closed },
                Rejected => Array.Empty<string>(),
                Converted => Array.Empty<string>(),
                Closed => Array.Empty<string>(),
                _ => Array.Empty<string>()
            };
        }

        /// <summary>Check if transition is valid</summary>
        public static bool IsValidTransition(string? currentStatus, string? newStatus)
        {
            if (string.IsNullOrEmpty(currentStatus) || string.IsNullOrEmpty(newStatus))
                return false;

            var validTransitions = GetValidTransitions(currentStatus);
            return validTransitions.Contains(newStatus);
        }

        /// <summary>Normalize legacy status values</summary>
        public static string Normalize(string? status)
        {
            if (string.IsNullOrEmpty(status)) return Pending;

            // Handle case variations
            var normalized = status.Trim();
            
            return normalized switch
            {
                "pending" => Pending,
                "approved" => Approved,
                "rejected" => Rejected,
                "converted" => Converted,
                "closed" => Closed,
                _ => normalized
            };
        }
    }
}
