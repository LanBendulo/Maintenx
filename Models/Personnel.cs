using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents workforce personnel (technicians, contractors, supervisors)
    /// Can exist with or without a user account
    /// </summary>
    [Table("Personnel")]
    public class Personnel
    {
        [Key]
        [Column("personnel_id")]
        public int PersonnelId { get; set; }

        [Column("company_id")]
        [Required]
        public int CompanyId { get; set; }

        [Column("user_id")]
        [StringLength(450)]
        public string? UserId { get; set; }

        [Column("first_name")]
        [StringLength(100)]
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        [StringLength(100)]
        [Required]
        public string LastName { get; set; } = string.Empty;

        [Column("role")]
        [StringLength(50)]
        public string? Role { get; set; }

        [Column("skill_set")]
        [StringLength(255)]
        public string? SkillSet { get; set; }

        [Column("hourly_rate", TypeName = "decimal(10,2)")]
        public decimal? HourlyRate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Computed property
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        // Navigation property to user account (optional)
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Navigation properties for work orders
        public virtual ICollection<WorkOrder> AssignedWorkOrders { get; set; } = new List<WorkOrder>();
        public virtual ICollection<WorkOrder> CreatedWorkOrders { get; set; } = new List<WorkOrder>();
    }
}
