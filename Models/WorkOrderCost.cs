using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents cost tracking for a work order
    /// Includes labor, parts, and other costs
    /// </summary>
    [Table("WorkOrderCost")]
    public class WorkOrderCost
    {
        [Key]
        [Column("cost_id")]
        public int CostId { get; set; }

        [Column("work_order_id")]
        [Required]
        public int WorkOrderId { get; set; }

        [Column("labor_cost", TypeName = "decimal(10,2)")]
        public decimal? LaborCost { get; set; } = 0;

        [Column("parts_cost", TypeName = "decimal(10,2)")]
        public decimal? PartsCost { get; set; } = 0;

        [Column("other_cost", TypeName = "decimal(10,2)")]
        public decimal? OtherCost { get; set; } = 0;

        [Column("total_cost", TypeName = "decimal(10,2)")]
        public decimal? TotalCost { get; set; } = 0;

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("WorkOrderId")]
        public virtual WorkOrder? WorkOrder { get; set; }
    }
}
