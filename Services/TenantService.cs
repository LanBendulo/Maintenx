using IT15_Project.Data;
using IT15_Project.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IT15_Project.Services
{
    /// <summary>
    /// Implementation of tenant service for multi-tenant isolation
    /// Manages company context for the current user
    /// </summary>
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TenantService(
            IHttpContextAccessor httpContextAccessor, 
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Gets the current user's CompanyId from their ApplicationUser record
        /// </summary>
        public int GetCurrentCompanyId()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                // If no user is authenticated, return default company (for seeding/admin operations)
                return 1;
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                // User not found, return default company
                return 1;
            }

            return user.CompanyId;
        }

        /// <summary>
        /// Gets the current user's Company information
        /// </summary>
        public async Task<Company?> GetCurrentCompanyAsync()
        {
            var companyId = GetCurrentCompanyId();
            return await _context.Companies.FindAsync(companyId);
        }

        /// <summary>
        /// Checks if the current user belongs to the specified company
        /// </summary>
        public bool BelongsToCompany(int companyId)
        {
            try
            {
                var currentCompanyId = GetCurrentCompanyId();
                return currentCompanyId == companyId;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that a record belongs to the current user's company
        /// Throws exception if access is denied
        /// </summary>
        public void ValidateCompanyAccess(int recordCompanyId)
        {
            var currentCompanyId = GetCurrentCompanyId();

            if (currentCompanyId != recordCompanyId)
            {
                throw new UnauthorizedAccessException("Access denied: Record belongs to a different company");
            }
        }
    }
}
