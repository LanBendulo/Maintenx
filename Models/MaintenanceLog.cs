using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    [Table("MaintenanceLog")]
    public class MaintenanceLog
    {
        [Key]
        [Column("log_id")]
        public int LogId { get; set; }

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [Column("work_order_id")]
        public int WorkOrderId { get; set; }

        [Column("asset_id")]
        public int? AssetId { get; set; }

        [Required]
        [Column("title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("completed_by_personnel_id")]
        public int? CompletedByPersonnelId { get; set; }

        [Required]
        [Column("completed_date")]
        public DateTime CompletedDate { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        // Cost tracking (snapshot from WorkOrderCost)
        [Column("labor_cost", TypeName = "decimal(10,2)")]
        public decimal? LaborCost { get; set; }

        [Column("parts_cost", TypeName = "decimal(10,2)")]
        public decimal? PartsCost { get; set; }

        [Column("other_cost", TypeName = "decimal(10,2)")]
        public decimal? OtherCost { get; set; }

        [Column("total_cost", TypeName = "decimal(10,2)")]
        public decimal? TotalCost { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("WorkOrderId")]
        public virtual WorkOrder? WorkOrder { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }

        [ForeignKey("CompletedByPersonnelId")]
        public virtual Personnel? CompletedByPersonnel { get; set; }
    }
}
