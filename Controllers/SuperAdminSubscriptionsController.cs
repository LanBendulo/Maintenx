using IT15_Project.Data;
using IT15_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// SuperAdmin Subscription Management
    /// Manage subscription plans and company subscriptions
    /// </summary>
    [Authorize(Roles = "SuperAdmin")]
    [Route("superadmin/subscriptions")]
    public class SuperAdminSubscriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuperAdminSubscriptionsController> _logger;

        public SuperAdminSubscriptionsController(
            ApplicationDbContext context,
            ILogger<SuperAdminSubscriptionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// List all subscription plans
        /// </summary>
        [HttpGet("plans")]
        public async Task<IActionResult> Plans()
        {
            try
            {
                var plans = await _context.SubscriptionPlans
                    .AsNoTracking()
                    .OrderBy(p => p.MonthlyPrice)
                    .ToListAsync();

                return View(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading subscription plans");
                return View("Error");
            }
        }

        /// <summary>
        /// Get all company subscriptions
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var subscriptions = await _context.CompanySubscriptions
                    .AsNoTracking()
                    .Include(s => s.Company)
                    .Include(s => s.Plan)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return View(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading company subscriptions");
                return View("Error");
            }
        }

        /// <summary>
        /// Create a new subscription plan
        /// </summary>
        [HttpPost("plans/create")]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request)
        {
            try
            {
                var plan = new SubscriptionPlan
                {
                    Name = request.Name,
                    Description = request.Description,
                    MonthlyPrice = request.MonthlyPrice,
                    YearlyPrice = request.YearlyPrice,
                    MaxUsers = request.MaxUsers,
                    MaxAssets = request.MaxAssets,
                    MaxWorkOrdersPerMonth = request.MaxWorkOrdersPerMonth,
                    FeaturesJson = request.FeaturesJson,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.SubscriptionPlans.Add(plan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription plan created: {PlanName}", plan.Name);
                return Ok(new { success = true, message = "Plan created successfully", planId = plan.PlanId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription plan");
                return BadRequest(new { success = false, message = "Failed to create plan" });
            }
        }

        /// <summary>
        /// Update a subscription plan
        /// </summary>
        [HttpPost("plans/{id}/update")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] UpdatePlanRequest request)
        {
            try
            {
                var plan = await _context.SubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    return NotFound();
                }

                plan.Name = request.Name ?? plan.Name;
                plan.Description = request.Description;
                plan.MonthlyPrice = request.MonthlyPrice ?? plan.MonthlyPrice;
                plan.YearlyPrice = request.YearlyPrice ?? plan.YearlyPrice;
                plan.MaxUsers = request.MaxUsers;
                plan.MaxAssets = request.MaxAssets;
                plan.MaxWorkOrdersPerMonth = request.MaxWorkOrdersPerMonth;
                plan.FeaturesJson = request.FeaturesJson;
                plan.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription plan updated: {PlanId}", id);
                return Ok(new { success = true, message = "Plan updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription plan {PlanId}", id);
                return BadRequest(new { success = false, message = "Failed to update plan" });
            }
        }

        /// <summary>
        /// Toggle plan active status
        /// </summary>
        [HttpPost("plans/{id}/toggle")]
        public async Task<IActionResult> TogglePlan(int id)
        {
            try
            {
                var plan = await _context.SubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    return NotFound();
                }

                plan.IsActive = !plan.IsActive;
                plan.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription plan {PlanId} toggled to {Status}", id, plan.IsActive);
                return Ok(new { success = true, isActive = plan.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling subscription plan {PlanId}", id);
                return BadRequest(new { success = false, message = "Failed to toggle plan" });
            }
        }

        /// <summary>
        /// Assign subscription to company
        /// </summary>
        [HttpPost("assign")]
        public async Task<IActionResult> AssignSubscription([FromBody] AssignSubscriptionRequest request)
        {
            try
            {
                var subscription = new CompanySubscription
                {
                    CompanyId = request.CompanyId,
                    PlanId = request.PlanId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsTrial = request.IsTrial,
                    IsActive = true,
                    PaymentStatus = request.PaymentStatus ?? "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.CompanySubscriptions.Add(subscription);

                // Update company subscription info
                var company = await _context.Companies.FindAsync(request.CompanyId);
                if (company != null)
                {
                    var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
                    company.SubscriptionPlan = plan?.Name;
                    company.SubscriptionExpiry = request.EndDate;
                    company.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription assigned to company {CompanyId}", request.CompanyId);
                return Ok(new { success = true, message = "Subscription assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning subscription");
                return BadRequest(new { success = false, message = "Failed to assign subscription" });
            }
        }

        /// <summary>
        /// Extend subscription
        /// </summary>
        [HttpPost("{id}/extend")]
        public async Task<IActionResult> ExtendSubscription(int id, [FromBody] ExtendSubscriptionRequest request)
        {
            try
            {
                var subscription = await _context.CompanySubscriptions
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.SubscriptionId == id);

                if (subscription == null)
                {
                    return NotFound();
                }

                subscription.EndDate = request.NewEndDate;
                subscription.UpdatedAt = DateTime.Now;

                // Update company expiry
                if (subscription.Company != null)
                {
                    subscription.Company.SubscriptionExpiry = request.NewEndDate;
                    subscription.Company.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription {SubscriptionId} extended to {NewEndDate}", id, request.NewEndDate);
                return Ok(new { success = true, message = "Subscription extended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending subscription {SubscriptionId}", id);
                return BadRequest(new { success = false, message = "Failed to extend subscription" });
            }
        }

        /// <summary>
        /// Update payment status
        /// </summary>
        [HttpPost("{id}/payment-status")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusRequest request)
        {
            try
            {
                var subscription = await _context.CompanySubscriptions.FindAsync(id);
                if (subscription == null)
                {
                    return NotFound();
                }

                subscription.PaymentStatus = request.PaymentStatus;
                subscription.LastPaymentDate = request.PaymentStatus == "Paid" ? DateTime.Now : subscription.LastPaymentDate;
                subscription.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription {SubscriptionId} payment status updated to {Status}", id, request.PaymentStatus);
                return Ok(new { success = true, message = "Payment status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for subscription {SubscriptionId}", id);
                return BadRequest(new { success = false, message = "Failed to update payment status" });
            }
        }

        /// <summary>
        /// API: Get all companies for dropdown
        /// </summary>
        [HttpGet("/api/companies")]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var companies = await _context.Companies
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CompanyName)
                    .Select(c => new
                    {
                        c.CompanyId,
                        c.CompanyName
                    })
                    .ToListAsync();

                return Ok(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading companies");
                return BadRequest(new { success = false, message = "Failed to load companies" });
            }
        }

        /// <summary>
        /// API: Get all subscription plans for dropdown
        /// </summary>
        [HttpGet("/api/subscription-plans")]
        public async Task<IActionResult> GetSubscriptionPlans()
        {
            try
            {
                var plans = await _context.SubscriptionPlans
                    .AsNoTracking()
                    .OrderBy(p => p.MonthlyPrice)
                    .Select(p => new
                    {
                        p.PlanId,
                        p.Name,
                        p.MonthlyPrice,
                        p.YearlyPrice,
                        p.IsActive
                    })
                    .ToListAsync();

                return Ok(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading subscription plans");
                return BadRequest(new { success = false, message = "Failed to load plans" });
            }
        }
    }

    public class CreatePlanRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public int? MaxUsers { get; set; }
        public int? MaxAssets { get; set; }
        public int? MaxWorkOrdersPerMonth { get; set; }
        public string? FeaturesJson { get; set; }
    }

    public class UpdatePlanRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? MonthlyPrice { get; set; }
        public decimal? YearlyPrice { get; set; }
        public int? MaxUsers { get; set; }
        public int? MaxAssets { get; set; }
        public int? MaxWorkOrdersPerMonth { get; set; }
        public string? FeaturesJson { get; set; }
    }

    public class AssignSubscriptionRequest
    {
        public int CompanyId { get; set; }
        public int PlanId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsTrial { get; set; }
        public string? PaymentStatus { get; set; }
    }

    public class ExtendSubscriptionRequest
    {
        public DateTime NewEndDate { get; set; }
    }

    public class UpdatePaymentStatusRequest
    {
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
