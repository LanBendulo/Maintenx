using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents parts used in a work order
    /// Junction table between WorkOrder and Part
    /// </summary>
    [Table("WorkOrderPart")]
    public class WorkOrderPart
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

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

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("WorkOrderId")]
        public virtual WorkOrder? WorkOrder { get; set; }

        [ForeignKey("PartId")]
        public virtual Part? Part { get; set; }
    }
}
