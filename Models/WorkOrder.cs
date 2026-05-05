using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents a maintenance work order
    /// </summary>
    [Table("Work_Order")]
    public class WorkOrder
    {
        [Key]
        [Column("work_order_id")]
        public int WorkOrderId { get; set; }

        [Column("asset_id")]
        public int? AssetId { get; set; }

        [Column("assigned_to")]
        public int? AssignedTo { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("status")]
        [StringLength(30)]
        public string? Status { get; set; }

        [Column("priority")]
        [StringLength(20)]
        public string? Priority { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("date_created")]
        [DataType(DataType.Date)]
        public DateTime? DateCreated { get; set; }

        [Column("due_date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Column("actual_completion")]
        [DataType(DataType.Date)]
        public DateTime? ActualCompletion { get; set; }

        [Column("maintenance_request_id")]
        public int? MaintenanceRequestId { get; set; }

        [Column("is_archived")]
        public bool IsArchived { get; set; } = false;

        [Column("archived_at")]
        public DateTime? ArchivedAt { get; set; }

        [Column("archived_by_user_id")]
        [StringLength(450)]
        public string? ArchivedByUserId { get; set; }

        // Navigation properties
        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual Personnel? AssignedToPersonnel { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual Personnel? CreatedByPersonnel { get; set; }

        [ForeignKey("MaintenanceRequestId")]
        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }

        [ForeignKey("ArchivedByUserId")]
        public virtual ApplicationUser? ArchivedByUser { get; set; }
    }
}
