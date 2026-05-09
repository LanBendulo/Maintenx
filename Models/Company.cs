using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents a Company (Tenant) in the multi-tenant SaaS architecture
    /// All business data is isolated by CompanyId
    /// </summary>
    [Table("Company")]
    public class Company
    {
        [Key]
        [Column("company_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CompanyId { get; set; }

        [Column("company_name")]
        [StringLength(200)]
        [Required]
        public string CompanyName { get; set; } = string.Empty;

        [Column("subscription_plan")]
        [StringLength(50)]
        public string? SubscriptionPlan { get; set; } = "Free";

        [Column("subscription_expiry")]
        public DateTime? SubscriptionExpiry { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Contact information
        [Column("contact_email")]
        [StringLength(255)]
        [EmailAddress]
        public string? ContactEmail { get; set; }

        [Column("contact_phone")]
        [StringLength(50)]
        public string? ContactPhone { get; set; }

        [Column("address")]
        [StringLength(500)]
        public string? Address { get; set; }

        // Billing information
        [Column("billing_email")]
        [StringLength(255)]
        [EmailAddress]
        public string? BillingEmail { get; set; }

        [Column("max_users")]
        public int? MaxUsers { get; set; } = 10;

        [Column("max_assets")]
        public int? MaxAssets { get; set; } = 100;

        // Navigation properties
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
        public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
        public virtual ICollection<Personnel> Personnel { get; set; } = new List<Personnel>();
        public virtual ICollection<CompanySubscription> Subscriptions { get; set; } = new List<CompanySubscription>();
    }
}
