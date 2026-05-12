using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents parts used in a work order
    /// Junction table between WorkOrder and Part
    /// Supports staged usage workflow with lifecycle governance
    /// </summary>
    [Table("WorkOrderPart")]
    public class WorkOrderPart
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("company_id")]
        [Required]
        public int CompanyId { get; set; }

        [Column("work_order_id")]
        [Required]
        public int WorkOrderId { get; set; }

        [Column("part_id")]
        [Required]
        public int PartId { get; set; }

        [Column("quantity_used")]
        [Required]
        public int QuantityUsed { get; set; }

        [Column("unit_cost", TypeName = "decimal(10,2)")]
        public decimal? UnitCost { get; set; }

        [Column("total_cost", TypeName = "decimal(10,2)")]
        public decimal? TotalCost { get; set; }

        /// <summary>
        /// Usage lifecycle status
        /// Pending: Staged by technician, not yet consumed
        /// Approved: Approved by supervisor (optional workflow)
        /// Consumed: Inventory deducted, finalized
        /// Rejected: Rejected, not consumed
        /// </summary>
        [Column("usage_status")]
        [StringLength(50)]
        public string UsageStatus { get; set; } = "Pending";

        /// <summary>
        /// Technician who added this part usage
        /// </summary>
        [Column("added_by_personnel_id")]
        public int? AddedByPersonnelId { get; set; }

        /// <summary>
        /// Supervisor/Admin who approved consumption (if applicable)
        /// </summary>
        [Column("approved_by_user_id")]
        [StringLength(450)]
        public string? ApprovedByUserId { get; set; }

        /// <summary>
        /// When inventory was actually consumed/deducted
        /// </summary>
        [Column("consumed_at")]
        public DateTime? ConsumedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("WorkOrderId")]
        public virtual WorkOrder? WorkOrder { get; set; }

        [ForeignKey("PartId")]
        public virtual Part? Part { get; set; }

        [ForeignKey("AddedByPersonnelId")]
        public virtual Personnel? AddedByPersonnel { get; set; }

        [ForeignKey("ApprovedByUserId")]
        public virtual ApplicationUser? ApprovedByUser { get; set; }
    }

    /// <summary>
    /// Parts usage lifecycle statuses
    /// </summary>
    public static class PartUsageStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Consumed = "Consumed";
        public const string Rejected = "Rejected";
    }
}
