namespace IT15_Project.Constants
{
    /// <summary>
    /// Centralized Asset Status Constants
    /// These are the canonical MaintenX asset lifecycle statuses.
    /// Use these constants throughout the system for consistency.
    /// </summary>
    public static class AssetStatuses
    {
        /// <summary>
        /// Asset is operational and available for use
        /// </summary>
        public const string Active = "Active";

        /// <summary>
        /// Asset has active work order(s) and is undergoing maintenance
        /// </summary>
        public const string UnderMaintenance = "Under Maintenance";

        /// <summary>
        /// Asset is not operational but not decommissioned
        /// </summary>
        public const string OutOfService = "Out of Service";

        /// <summary>
        /// Asset is decommissioned and no longer in service
        /// </summary>
        public const string Retired = "Retired";

        /// <summary>
        /// Returns all valid asset statuses
        /// </summary>
        public static string[] GetAll() => new[]
        {
            Active,
            UnderMaintenance,
            OutOfService,
            Retired
        };

        /// <summary>
        /// Validates if a status string is a valid asset status
        /// </summary>
        public static bool IsValid(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status == Active ||
                   status == UnderMaintenance ||
                   status == OutOfService ||
                   status == Retired;
        }
    }
}
