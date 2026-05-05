using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents a preventive maintenance schedule for an asset
    /// Automatically generates work orders based on frequency
    /// MULTI-TENANT: All queries must filter by CompanyId
    /// </summary>
    [Table("PreventiveSchedule")]
    public class PreventiveSchedule
    {
        [Key]
        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        [Column("company_id")]
        [Required]
        public int CompanyId { get; set; }

        [Column("asset_id")]
        [Required]
        public int AssetId { get; set; }

        [Column("title")]
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("frequency_days")]
        [Required]
        public int FrequencyDays { get; set; }

        [Column("next_due_date")]
        [Required]
        [DataType(DataType.Date)]
        public DateTime NextDueDate { get; set; }

        [Column("last_completed_date")]
        [DataType(DataType.Date)]
        public DateTime? LastCompletedDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("default_technician_id")]
        public int? DefaultTechnicianId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }

        [ForeignKey("DefaultTechnicianId")]
        public virtual Personnel? DefaultTechnician { get; set; }
    }
}
