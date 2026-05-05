using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents a maintenance request - the entry point of the CMMS workflow
    /// </summary>
    [Table("Maintenance_Request")]
    public class MaintenanceRequest
    {
        [Key]
        [Column("request_id")]
        public int RequestId { get; set; }

        [Column("request_number")]
        [StringLength(50)]
        [Required]
        public string RequestNumber { get; set; } = string.Empty;

        [Column("title")]
        [StringLength(100)]
        [Required]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        [Required]
        public string Description { get; set; } = string.Empty;

        [Column("asset_id")]
        [Required]
        public int AssetId { get; set; }

        [Column("priority")]
        [StringLength(20)]
        [Required]
        public string Priority { get; set; } = "Medium";

        [Column("status")]
        [StringLength(30)]
        [Required]
        public string Status { get; set; } = "Pending";

        [Column("requested_by")]
        public int? RequestedBy { get; set; }

        [Column("category")]
        [StringLength(50)]
        public string? Category { get; set; }

        [Column("location")]
        [StringLength(200)]
        public string? Location { get; set; }

        [Column("attachment_url")]
        [StringLength(500)]
        public string? AttachmentUrl { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

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

        [ForeignKey("RequestedBy")]
        public virtual Personnel? RequestedByPersonnel { get; set; }

        [ForeignKey("ArchivedByUserId")]
        public virtual ApplicationUser? ArchivedByUser { get; set; }

        // One-to-one relationship with WorkOrder
        public virtual WorkOrder? WorkOrder { get; set; }
    }
}
