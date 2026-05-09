using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents workforce personnel (technicians, contractors, supervisors)
    /// Can exist with or without a user account
    /// Separate from ApplicationUser - this is business/workforce entity
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

        [Column("employee_id")]
        [StringLength(50)]
        public string? EmployeeId { get; set; }

        [Column("first_name")]
        [StringLength(100)]
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Column("middle_name")]
        [StringLength(100)]
        public string? MiddleName { get; set; }

        [Column("last_name")]
        [StringLength(100)]
        [Required]
        public string LastName { get; set; } = string.Empty;

        [Column("email")]
        [StringLength(256)]
        [EmailAddress]
        public string? Email { get; set; }

        [Column("phone_number")]
        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        [Column("address")]
        [StringLength(500)]
        public string? Address { get; set; }

        [Column("position")]
        [StringLength(100)]
        public string? Position { get; set; }

        [Column("department")]
        [StringLength(100)]
        public string? Department { get; set; }

        [Column("employment_type")]
        [StringLength(50)]
        public string? EmploymentType { get; set; } // FullTime, PartTime, Contractor, Intern, Temporary

        [Column("hire_date")]
        public DateTime? HireDate { get; set; }

        [Column("termination_date")]
        public DateTime? TerminationDate { get; set; }

        [Column("status")]
        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Inactive, OnLeave, Retired, Terminated

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("role")]
        [StringLength(50)]
        public string? Role { get; set; } // Job role: Technician, Supervisor, etc.

        [Column("skill_set")]
        [StringLength(255)]
        public string? SkillSet { get; set; }

        [Column("hourly_rate", TypeName = "decimal(10,2)")]
        public decimal? HourlyRate { get; set; }

        [Column("emergency_contact_name")]
        [StringLength(200)]
        public string? EmergencyContactName { get; set; }

        [Column("emergency_contact_phone")]
        [StringLength(50)]
        public string? EmergencyContactPhone { get; set; }

        [Column("profile_photo_url")]
        [StringLength(500)]
        public string? ProfilePhotoUrl { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Computed properties
        [NotMapped]
        public string FullName
        {
            get
            {
                var parts = new[] { FirstName, MiddleName, LastName }
                    .Where(p => !string.IsNullOrWhiteSpace(p));
                return string.Join(" ", parts);
            }
        }

        [NotMapped]
        public bool HasUserAccount => !string.IsNullOrEmpty(UserId);

        [NotMapped]
        public bool IsTerminated => Status == "Terminated" || TerminationDate.HasValue;

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

    /// <summary>
    /// Employment Type Enum
    /// </summary>
    public static class EmploymentType
    {
        public const string FullTime = "FullTime";
        public const string PartTime = "PartTime";
        public const string Contractor = "Contractor";
        public const string Intern = "Intern";
        public const string Temporary = "Temporary";

        public static List<string> GetAll() => new()
        {
            FullTime, PartTime, Contractor, Intern, Temporary
        };
    }

    /// <summary>
    /// Personnel Status Enum
    /// </summary>
    public static class PersonnelStatus
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";
        public const string OnLeave = "OnLeave";
        public const string Retired = "Retired";
        public const string Terminated = "Terminated";

        public static List<string> GetAll() => new()
        {
            Active, Inactive, OnLeave, Retired, Terminated
        };
    }
}
