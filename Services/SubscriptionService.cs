using IT15_Project.Data;
using IT15_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_Project.Services
{
    /// <summary>
    /// Service for subscription management and enforcement
    /// Provides lightweight plan limit checking
    /// </summary>
    public class SubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            ApplicationDbContext context,
            ILogger<SubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get active subscription for a company
        /// </summary>
        public async Task<CompanySubscription?> GetActiveSubscriptionAsync(int companyId)
        {
            return await _context.CompanySubscriptions
                .Include(s => s.Plan)
                .Include(s => s.Company)
                .Where(s => s.CompanyId == companyId && s.IsActive && s.EndDate > DateTime.Now)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Check if company can add more users
        /// </summary>
        public async Task<(bool allowed, string? message)> CanAddUserAsync(int companyId)
        {
            var subscription = await GetActiveSubscriptionAsync(companyId);
            if (subscription?.Plan == null)
            {
                return (false, "No active subscription found");
            }

            if (!subscription.Plan.MaxUsers.HasValue)
            {
                return (true, null); // Unlimited
            }

            var currentUserCount = await _context.Users
                .CountAsync(u => u.CompanyId == companyId);

            if (currentUserCount >= subscription.Plan.MaxUsers.Value)
            {
                return (false, $"User limit reached ({subscription.Plan.MaxUsers} users). Please upgrade your plan.");
            }

            return (true, null);
        }

        /// <summary>
        /// Check if company can add more assets
        /// </summary>
        public async Task<(bool allowed, string? message)> CanAddAssetAsync(int companyId)
        {
            var subscription = await GetActiveSubscriptionAsync(companyId);
            if (subscription?.Plan == null)
            {
                return (false, "No active subscription found");
            }

            if (!subscription.Plan.MaxAssets.HasValue)
            {
                return (true, null); // Unlimited
            }

            var currentAssetCount = await _context.Assets
                .CountAsync(a => a.CompanyId == companyId);

            if (currentAssetCount >= subscription.Plan.MaxAssets.Value)
            {
                return (false, $"Asset limit reached ({subscription.Plan.MaxAssets} assets). Please upgrade your plan.");
            }

            return (true, null);
        }

        /// <summary>
        /// Check if company can create more work orders this month
        /// </summary>
        public async Task<(bool allowed, string? message)> CanCreateWorkOrderAsync(int companyId)
        {
            var subscription = await GetActiveSubscriptionAsync(companyId);
            if (subscription?.Plan == null)
            {
                return (false, "No active subscription found");
            }

            if (!subscription.Plan.MaxWorkOrdersPerMonth.HasValue)
            {
                return (true, null); // Unlimited
            }

            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var currentMonthCount = await _context.WorkOrders
                .CountAsync(w => w.CompanyId == companyId && w.DateCreated >= startOfMonth);

            if (currentMonthCount >= subscription.Plan.MaxWorkOrdersPerMonth.Value)
            {
                return (false, $"Work order limit reached ({subscription.Plan.MaxWorkOrdersPerMonth} per month). Please upgrade your plan.");
            }

            return (true, null);
        }

        /// <summary>
        /// Get subscription status for dashboard banner
        /// </summary>
        public async Task<SubscriptionStatus?> GetSubscriptionStatusAsync(int companyId)
        {
            var subscription = await GetActiveSubscriptionAsync(companyId);
            if (subscription == null)
            {
                return new SubscriptionStatus
                {
                    IsActive = false,
                    Message = "No active subscription",
                    Severity = "error"
                };
            }

            var daysRemaining = (subscription.EndDate - DateTime.Now).Days;

            if (daysRemaining <= 0)
            {
                return new SubscriptionStatus
                {
                    IsActive = false,
                    Message = "Subscription expired",
                    Severity = "error",
                    DaysRemaining = 0
                };
            }

            if (subscription.IsTrial && daysRemaining <= 7)
            {
                return new SubscriptionStatus
                {
                    IsActive = true,
                    Message = $"Trial expires in {daysRemaining} day{(daysRemaining != 1 ? "s" : "")}",
                    Severity = "warning",
                    DaysRemaining = daysRemaining,
                    IsTrial = true
                };
            }

            if (daysRemaining <= 30)
            {
                return new SubscriptionStatus
                {
                    IsActive = true,
                    Message = $"Subscription expires in {daysRemaining} day{(daysRemaining != 1 ? "s" : "")}",
                    Severity = "warning",
                    DaysRemaining = daysRemaining
                };
            }

            return new SubscriptionStatus
            {
                IsActive = true,
                Message = "Subscription active",
                Severity = "success",
                DaysRemaining = daysRemaining
            };
        }

        /// <summary>
        /// Get usage statistics for a company
        /// </summary>
        public async Task<SubscriptionUsage> GetUsageStatsAsync(int companyId)
        {
            var subscription = await GetActiveSubscriptionAsync(companyId);
            
            var userCount = await _context.Users.CountAsync(u => u.CompanyId == companyId);
            var assetCount = await _context.Assets.CountAsync(a => a.CompanyId == companyId);
            
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var workOrderCount = await _context.WorkOrders
                .CountAsync(w => w.CompanyId == companyId && w.DateCreated >= startOfMonth);

            return new SubscriptionUsage
            {
                UserCount = userCount,
                MaxUsers = subscription?.Plan?.MaxUsers,
                AssetCount = assetCount,
                MaxAssets = subscription?.Plan?.MaxAssets,
                WorkOrderCount = workOrderCount,
                MaxWorkOrders = subscription?.Plan?.MaxWorkOrdersPerMonth,
                PlanName = subscription?.Plan?.Name ?? "No Plan"
            };
        }
    }

    public class SubscriptionStatus
    {
        public bool IsActive { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "info"; // success, warning, error, info
        public int DaysRemaining { get; set; }
        public bool IsTrial { get; set; }
    }

    public class SubscriptionUsage
    {
        public int UserCount { get; set; }
        public int? MaxUsers { get; set; }
        public int AssetCount { get; set; }
        public int? MaxAssets { get; set; }
        public int WorkOrderCount { get; set; }
        public int? MaxWorkOrders { get; set; }
        public string PlanName { get; set; } = string.Empty;

        public int UserPercentage => MaxUsers.HasValue && MaxUsers.Value > 0 
            ? (int)((double)UserCount / MaxUsers.Value * 100) 
            : 0;

        public int AssetPercentage => MaxAssets.HasValue && MaxAssets.Value > 0 
            ? (int)((double)AssetCount / MaxAssets.Value * 100) 
            : 0;

        public int WorkOrderPercentage => MaxWorkOrders.HasValue && MaxWorkOrders.Value > 0 
            ? (int)((double)WorkOrderCount / MaxWorkOrders.Value * 100) 
            : 0;
    }
}
