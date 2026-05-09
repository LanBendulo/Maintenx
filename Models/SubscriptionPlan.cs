using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents a subscription plan available for companies
    /// Platform-level entity managed by SuperAdmin
    /// </summary>
    [Table("SubscriptionPlan")]
    public class SubscriptionPlan
    {
        [Key]
        [Column("plan_id")]
        public int PlanId { get; set; }

        [Column("name")]
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Column("monthly_price", TypeName = "decimal(10,2)")]
        public decimal MonthlyPrice { get; set; }

        [Column("yearly_price", TypeName = "decimal(10,2)")]
        public decimal YearlyPrice { get; set; }

        [Column("max_users")]
        public int? MaxUsers { get; set; }

        [Column("max_assets")]
        public int? MaxAssets { get; set; }

        [Column("max_work_orders_per_month")]
        public int? MaxWorkOrdersPerMonth { get; set; }

        [Column("features_json")]
        public string? FeaturesJson { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<CompanySubscription> CompanySubscriptions { get; set; } = new List<CompanySubscription>();
    }
}
