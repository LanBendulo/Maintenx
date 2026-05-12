using System.Collections.Generic;

namespace IT15_Project.Models.ViewModels
{
    /// <summary>
    /// ViewModel for displaying subscription plans on the public landing page
    /// Contains only public-facing information, no sensitive admin data
    /// </summary>
    public class PublicPricingViewModel
    {
        public List<PublicPlanDto> Plans { get; set; } = new List<PublicPlanDto>();
    }

    /// <summary>
    /// Public-facing subscription plan data transfer object
    /// Safe for public display on landing page
    /// </summary>
    public class PublicPlanDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public int? MaxUsers { get; set; }
        public int? MaxAssets { get; set; }
        public int? MaxWorkOrdersPerMonth { get; set; }
        public List<string> Features { get; set; } = new List<string>();
        
        // Computed properties for display
        public bool IsFree => MonthlyPrice == 0 && YearlyPrice == 0;
        public bool IsCustomPricing => MonthlyPrice == 0 && YearlyPrice == 0 && Name.Contains("Enterprise", StringComparison.OrdinalIgnoreCase);
        public bool HasUnlimitedUsers => !MaxUsers.HasValue;
        public bool HasUnlimitedAssets => !MaxAssets.HasValue;
        public bool HasUnlimitedWorkOrders => !MaxWorkOrdersPerMonth.HasValue;
        
        // Display helpers
        public string DisplayMaxUsers => MaxUsers.HasValue ? MaxUsers.Value.ToString() : "Unlimited";
        public string DisplayMaxAssets => MaxAssets.HasValue ? MaxAssets.Value.ToString() : "Unlimited";
        public string DisplayMaxWorkOrders => MaxWorkOrdersPerMonth.HasValue ? MaxWorkOrdersPerMonth.Value.ToString() : "Unlimited";
    }
}
