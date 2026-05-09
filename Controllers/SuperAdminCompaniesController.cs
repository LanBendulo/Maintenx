using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// SuperAdmin Company Management
    /// Manage all companies in the platform
    /// </summary>
    [Authorize(Roles = "SuperAdmin")]
    [Route("superadmin/companies")]
    public class SuperAdminCompaniesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuperAdminCompaniesController> _logger;

        public SuperAdminCompaniesController(
            ApplicationDbContext context,
            ILogger<SuperAdminCompaniesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// List all companies
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var companies = await _context.Companies
                    .AsNoTracking()
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.CompanyId,
                        c.CompanyName,
                        c.IsActive,
                        c.SubscriptionPlan,
                        c.SubscriptionExpiry,
                        c.MaxUsers,
                        c.MaxAssets,
                        c.ContactEmail,
                        c.CreatedAt,
                        UserCount = c.Users.Count,
                        AssetCount = c.Assets.Count,
                        WorkOrderCount = c.WorkOrders.Count
                    })
                    .ToListAsync();

                return View(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading companies list");
                return View("Error");
            }
        }

        /// <summary>
        /// Get company details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var company = await _context.Companies
                    .AsNoTracking()
                    .Include(c => c.Users)
                    .Include(c => c.Subscriptions)
                        .ThenInclude(s => s.Plan)
                    .FirstOrDefaultAsync(c => c.CompanyId == id);

                if (company == null)
                {
                    return NotFound();
                }

                // Get metrics
                var userCount = await _context.Users.CountAsync(u => u.CompanyId == id);
                var assetCount = await _context.Assets.CountAsync(a => a.CompanyId == id);
                var workOrderCount = await _context.WorkOrders.CountAsync(w => w.CompanyId == id);
                var maintenanceRequestCount = await _context.MaintenanceRequests.CountAsync(m => m.CompanyId == id);
                var technicianCount = await _context.Personnel
                    .CountAsync(p => p.CompanyId == id && p.Role == "Technician" && p.IsActive);

                // Get recent work orders
                var recentWorkOrders = await _context.WorkOrders
                    .AsNoTracking()
                    .Where(w => w.CompanyId == id)
                    .OrderByDescending(w => w.DateCreated)
                    .Take(5)
                    .Select(w => new RecentWorkOrderDto
                    {
                        WorkOrderId = w.WorkOrderId,
                        WorkOrderNumber = w.WorkOrderId.ToString(),
                        Title = w.Description ?? "No description",
                        Status = w.Status ?? "Unknown",
                        Priority = w.Priority ?? "Medium",
                        DateCreated = w.DateCreated ?? DateTime.Now,
                        AssetName = w.Asset != null ? w.Asset.AssetName : "N/A"
                    })
                    .ToListAsync();

                // Get recent maintenance requests
                var recentRequests = await _context.MaintenanceRequests
                    .AsNoTracking()
                    .Where(m => m.CompanyId == id)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(5)
                    .Select(m => new RecentMaintenanceRequestDto
                    {
                        RequestId = m.RequestId,
                        RequestNumber = m.RequestNumber,
                        IssueDescription = m.Title,
                        Status = m.Status,
                        Priority = m.Priority,
                        CreatedAt = m.CreatedAt,
                        AssetName = m.Asset != null ? m.Asset.AssetName : "N/A"
                    })
                    .ToListAsync();

                // Get recent users
                var recentUsers = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.CompanyId == id)
                    .OrderByDescending(u => u.Id)
                    .Take(5)
                    .Select(u => new RecentUserDto
                    {
                        Id = u.Id,
                        FullName = u.FullName ?? "N/A",
                        Email = u.Email ?? "N/A",
                        EmailConfirmed = u.EmailConfirmed,
                        Personnel = _context.Personnel
                            .Where(p => p.UserId == u.Id)
                            .Select(p => p.Role)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                // Calculate subscription days remaining
                int? daysRemaining = null;
                string subscriptionStatus = "Active";
                
                if (company.SubscriptionExpiry.HasValue)
                {
                    daysRemaining = (company.SubscriptionExpiry.Value - DateTime.Now).Days;
                    
                    if (daysRemaining < 0)
                    {
                        subscriptionStatus = "Expired";
                    }
                    else if (daysRemaining <= 30)
                    {
                        subscriptionStatus = "Expiring";
                    }
                }

                var viewModel = new SuperAdminCompanyDetailsViewModel
                {
                    Company = company,
                    UserCount = userCount,
                    AssetCount = assetCount,
                    WorkOrderCount = workOrderCount,
                    MaintenanceRequestCount = maintenanceRequestCount,
                    TechnicianCount = technicianCount,
                    DaysRemaining = daysRemaining,
                    SubscriptionStatus = subscriptionStatus,
                    RecentWorkOrders = recentWorkOrders,
                    RecentRequests = recentRequests,
                    RecentUsers = recentUsers
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading company details for ID: {CompanyId}", id);
                return View("Error");
            }
        }

        /// <summary>
        /// Suspend a company
        /// </summary>
        [HttpPost("{id}/suspend")]
        public async Task<IActionResult> Suspend(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    return NotFound();
                }

                company.IsActive = false;
                company.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Company {CompanyId} suspended by SuperAdmin", id);
                return Ok(new { success = true, message = "Company suspended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending company {CompanyId}", id);
                return BadRequest(new { success = false, message = "Failed to suspend company" });
            }
        }

        /// <summary>
        /// Reactivate a company
        /// </summary>
        [HttpPost("{id}/reactivate")]
        public async Task<IActionResult> Reactivate(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    return NotFound();
                }

                company.IsActive = true;
                company.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Company {CompanyId} reactivated by SuperAdmin", id);
                return Ok(new { success = true, message = "Company reactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating company {CompanyId}", id);
                return BadRequest(new { success = false, message = "Failed to reactivate company" });
            }
        }

        /// <summary>
        /// Update company details
        /// </summary>
        [HttpPost("{id}/update")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCompanyRequest request)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    return NotFound();
                }

                company.CompanyName = request.CompanyName ?? company.CompanyName;
                company.ContactEmail = request.ContactEmail;
                company.ContactPhone = request.ContactPhone;
                company.Address = request.Address;
                company.MaxUsers = request.MaxUsers;
                company.MaxAssets = request.MaxAssets;
                company.SubscriptionPlan = request.SubscriptionPlan;
                company.SubscriptionExpiry = request.SubscriptionExpiry;
                company.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company {CompanyId} updated by SuperAdmin", id);
                return Ok(new { success = true, message = "Company updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {CompanyId}", id);
                return BadRequest(new { success = false, message = "Failed to update company" });
            }
        }

        /// <summary>
        /// Delete company (soft delete)
        /// </summary>
        [HttpPost("{id}/delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    return NotFound();
                }

                // Soft delete: just deactivate
                company.IsActive = false;
                company.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Company {CompanyId} deleted (soft) by SuperAdmin", id);
                return Ok(new { success = true, message = "Company deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company {CompanyId}", id);
                return BadRequest(new { success = false, message = "Failed to delete company" });
            }
        }
    }

    public class UpdateCompanyRequest
    {
        public string? CompanyName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public int? MaxUsers { get; set; }
        public int? MaxAssets { get; set; }
        public string? SubscriptionPlan { get; set; }
        public DateTime? SubscriptionExpiry { get; set; }
    }
}
