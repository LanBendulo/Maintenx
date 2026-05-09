using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Tracks Asset operational status changes for audit trail
    /// </summary>
    [Table("AssetStatusHistory")]
    public class AssetStatusHistory
    {
        [Key]
        [Column("history_id")]
        public int HistoryId { get; set; }

        [Column("asset_id")]
        [Required]
        public int AssetId { get; set; }

        [Column("company_id")]
        [Required]
        public int CompanyId { get; set; }

        [Column("old_status")]
        [StringLength(30)]
        public string? OldStatus { get; set; }

        [Column("new_status")]
        [StringLength(30)]
        [Required]
        public string NewStatus { get; set; } = string.Empty;

        [Column("changed_by_user_id")]
        [StringLength(450)]
        public string? ChangedByUserId { get; set; }

        [Column("work_order_id")]
        public int? WorkOrderId { get; set; }

        [Column("reason")]
        [StringLength(500)]
        public string? Reason { get; set; }

        [Column("changed_at")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("WorkOrderId")]
        public virtual WorkOrder? WorkOrder { get; set; }

        [ForeignKey("ChangedByUserId")]
        public virtual ApplicationUser? ChangedByUser { get; set; }
    }
}
