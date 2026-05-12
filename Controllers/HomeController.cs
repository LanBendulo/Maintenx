using System.Diagnostics;
using System.Text.Json;
using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Load active subscription plans from database
                var plans = await _context.SubscriptionPlans
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.MonthlyPrice) // Order by price: Free -> Paid -> Custom
                    .Select(p => new PublicPlanDto
                    {
                        Name = p.Name,
                        Description = p.Description,
                        MonthlyPrice = p.MonthlyPrice,
                        YearlyPrice = p.YearlyPrice,
                        MaxUsers = p.MaxUsers,
                        MaxAssets = p.MaxAssets,
                        MaxWorkOrdersPerMonth = p.MaxWorkOrdersPerMonth,
                        Features = ParseFeatures(p.FeaturesJson)
                    })
                    .ToListAsync();

                var viewModel = new PublicPricingViewModel
                {
                    Plans = plans
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading subscription plans for landing page");
                
                // Return view with empty plans list as fallback
                return View(new PublicPricingViewModel());
            }
        }

        /// <summary>
        /// Safely parse features JSON into a list of strings
        /// Returns empty list if JSON is invalid or null
        /// </summary>
        private List<string> ParseFeatures(string? featuresJson)
        {
            if (string.IsNullOrWhiteSpace(featuresJson))
            {
                return new List<string>();
            }

            try
            {
                // Try to parse as array of strings
                var features = JsonSerializer.Deserialize<List<string>>(featuresJson);
                return features ?? new List<string>();
            }
            catch
            {
                try
                {
                    // Try to parse as object with feature keys
                    var featuresDict = JsonSerializer.Deserialize<Dictionary<string, object>>(featuresJson);
                    if (featuresDict != null)
                    {
                        return featuresDict.Keys.ToList();
                    }
                }
                catch
                {
                    // If all parsing fails, return empty list
                    _logger.LogWarning("Failed to parse features JSON: {FeaturesJson}", featuresJson);
                }
            }

            return new List<string>();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
