namespace IT15_Project.Services
{
    /// <summary>
    /// Service interface for tenant (company) context management
    /// Provides the current user's CompanyId for multi-tenant isolation
    /// </summary>
    public interface ITenantService
    {
        /// <summary>
        /// Gets the current user's CompanyId
        /// </summary>
        /// <returns>CompanyId of the current authenticated user</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated or has no company</exception>
        int GetCurrentCompanyId();

        /// <summary>
        /// Gets the current user's Company information
        /// </summary>
        /// <returns>Company entity or null if not found</returns>
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
