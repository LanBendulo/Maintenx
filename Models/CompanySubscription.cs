using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents a company's subscription to a plan
    /// Links Company to SubscriptionPlan with billing details
    /// </summary>
    [Table("CompanySubscription")]
    public class CompanySubscription
    {
        [Key]
        [Column("subscription_id")]
        public int SubscriptionId { get; set; }

        [Column("company_id")]
        [Required]
        public int CompanyId { get; set; }

        [Column("plan_id")]
        [Required]
        public int PlanId { get; set; }

        [Column("start_date")]
        [Required]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        [Required]
        public DateTime EndDate { get; set; }

        [Column("is_trial")]
        public bool IsTrial { get; set; } = false;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("payment_status")]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed, Cancelled

        [Column("external_payment_id")]
        [StringLength(200)]
        public string? ExternalPaymentId { get; set; }

        [Column("last_payment_date")]
        public DateTime? LastPaymentDate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Computed properties
        [NotMapped]
        public string Status
        {
            get
            {
                if (!IsActive) return "Inactive";
                if (EndDate <= DateTime.Now) return "Expired";
                if (IsTrial && EndDate <= DateTime.Now.AddDays(7)) return "Trial Ending";
                if (IsTrial) return "Trial";
                if (EndDate <= DateTime.Now.AddDays(30)) return "Expiring Soon";
                return "Active";
            }
        }

        [NotMapped]
        public int DaysRemaining
        {
            get
            {
                return (EndDate - DateTime.Now).Days;
            }
        }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("PlanId")]
        public virtual SubscriptionPlan? Plan { get; set; }
    }
}
