using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents a spare part or inventory item
    /// </summary>
    [Table("Part")]
    public class Part
    {
        [Key]
        [Column("part_id")]
        public int PartId { get; set; }

        [Column("company_id")]
        [Required]
        public int CompanyId { get; set; }

        [Column("part_name")]
        [StringLength(200)]
        [Required]
        public string PartName { get; set; } = string.Empty;

        [Column("part_number")]
        [StringLength(100)]
        public string? PartNumber { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("quantity")]
        [Required]
        public int Quantity { get; set; } = 0;

        [Column("unit_cost", TypeName = "decimal(10,2)")]
        public decimal? UnitCost { get; set; }

        [Column("reorder_level")]
        public int? ReorderLevel { get; set; }

        [Column("location")]
        [StringLength(200)]
        public string? Location { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public virtual ICollection<WorkOrderPart> WorkOrderParts { get; set; } = new List<WorkOrderPart>();
    }
}
