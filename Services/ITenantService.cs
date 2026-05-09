namespace IT15_Project.Services
{
    /// <summary>
    /// Service interface for tenant (company) context management
    /// Provides the current user's CompanyId for multi-tenant isolation
    /// Supports SuperAdmin (CompanyId = null) for platform-level access
    /// </summary>
    public interface ITenantService
    {
        /// <summary>
        /// Gets the current user's CompanyId (nullable for SuperAdmin support)
        /// Returns null for SuperAdmin users (platform-level access)
        /// Returns CompanyId for tenant users (company-scoped access)
        /// </summary>
        /// <returns>CompanyId or null for SuperAdmin</returns>
        int? GetCurrentCompanyIdNullable();

        /// <summary>
        /// Gets the current user's CompanyId
        /// Returns 1 as fallback for backward compatibility
        /// Use GetCurrentCompanyIdNullable() for SuperAdmin-aware code
        /// </summary>
        /// <returns>CompanyId of the current authenticated user</returns>
        int GetCurrentCompanyId();

        /// <summary>
        /// Checks if the current user is a SuperAdmin (platform-level access)
        /// </summary>
        /// <returns>True if user is SuperAdmin (CompanyId = null), false otherwise</returns>
        bool IsSuperAdmin();

        /// <summary>
        /// Gets the current user's Company information
        /// Returns null for SuperAdmin users
        /// </summary>
        /// <returns>Company entity or null if not found or SuperAdmin</returns>
        Task<Models.Company?> GetCurrentCompanyAsync();

        /// <summary>
        /// Checks if the current user belongs to the specified company
        /// </summary>
        /// <param name="companyId">Company ID to check</param>
        /// <returns>True if user belongs to the company, false otherwise</returns>
        bool BelongsToCompany(int companyId);

        /// <summary>
        /// Validates that a record belongs to the current user's company
        /// </summary>
        /// <param name="recordCompanyId">CompanyId of the record being accessed</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when record doesn't belong to user's company</exception>
        void ValidateCompanyAccess(int recordCompanyId);
    }
}
