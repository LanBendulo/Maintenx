using IT15_Project.Data;
using IT15_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// SuperAdmin Dashboard - Platform-level metrics and management
    /// Only accessible by SuperAdmin role (CompanyId = null)
    /// </summary>
    [Authorize(Roles = "SuperAdmin")]
    [Route("superadmin")]
    public class SuperAdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuperAdminDashboardController> _logger;

        public SuperAdminDashboardController(
            ApplicationDbContext context,
            ILogger<SuperAdminDashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// SuperAdmin Dashboard - Platform overview
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Summary metrics
                var totalCompanies = await _context.Companies.CountAsync();
                var activeCompanies = await _context.Companies.CountAsync(c => c.IsActive);
                var totalUsers = await _context.Users.CountAsync();
                var totalWorkOrders = await _context.WorkOrders.CountAsync();
                var activeSubscriptions = await _context.CompanySubscriptions
                    .CountAsync(s => s.IsActive && s.EndDate > DateTime.Now);

                // Recent companies (last 10)
                var recentCompanies = await _context.Companies
                    .AsNoTracking()
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(10)
                    .Select(c => new
                    {
                        c.CompanyId,
                        c.CompanyName,
                        c.IsActive,
                        c.SubscriptionPlan,
                        c.SubscriptionExpiry,
                        c.CreatedAt,
                        UserCount = c.Users.Count,
                        AssetCount = c.Assets.Count
                    })
                    .ToListAsync();

                // Expiring subscriptions (within 30 days)
                var expiringSubscriptions = await _context.Companies
                    .AsNoTracking()
                    .Where(c => c.IsActive && 
                                c.SubscriptionExpiry.HasValue && 
                                c.SubscriptionExpiry.Value <= DateTime.Now.AddDays(30) &&
                                c.SubscriptionExpiry.Value > DateTime.Now)
                    .OrderBy(c => c.SubscriptionExpiry)
                    .Take(10)
                    .Select(c => new
                    {
                        c.CompanyId,
                        c.CompanyName,
                        c.SubscriptionPlan,
                        c.SubscriptionExpiry,
                        DaysRemaining = c.SubscriptionExpiry.HasValue 
                            ? EF.Functions.DateDiffDay(DateTime.Now, c.SubscriptionExpiry.Value)
                            : 0
                    })
                    .ToListAsync();

                // Suspended companies
                var suspendedCompanies = await _context.Companies
                    .AsNoTracking()
                    .Where(c => !c.IsActive)
                    .OrderByDescending(c => c.UpdatedAt)
                    .Take(10)
                    .Select(c => new
                    {
                        c.CompanyId,
                        c.CompanyName,
                        c.SubscriptionPlan,
                        c.UpdatedAt
                    })
                    .ToListAsync();

                // Subscription plan distribution
                var planDistribution = await _context.Companies
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .GroupBy(c => c.SubscriptionPlan ?? "Free")
                    .Select(g => new
                    {
                        Plan = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var viewModel = new
                {
                    TotalCompanies = totalCompanies,
                    ActiveCompanies = activeCompanies,
                    TotalUsers = totalUsers,
                    TotalWorkOrders = totalWorkOrders,
                    ActiveSubscriptions = activeSubscriptions,
                    RecentCompanies = recentCompanies,
                    ExpiringSubscriptions = expiringSubscriptions,
                    SuspendedCompanies = suspendedCompanies,
                    PlanDistribution = planDistribution
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading SuperAdmin dashboard");
                return View("Error");
            }
        }
    }
}
