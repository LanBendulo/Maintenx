using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Immutable audit log for all inventory stock changes
    /// Provides complete traceability for inventory mutations
    /// </summary>
    [Table("InventoryMovement")]
    public class InventoryMovement
    {
        [Key]
        [Column("movement_id")]
        public int MovementId { get; set; }

        [Column("company_id")]
        [Required]
        public int CompanyId { get; set; }

        [Column("part_id")]
        [Required]
        public int PartId { get; set; }

        /// <summary>
        /// Quantity change (positive for additions, negative for deductions)
        /// </summary>
        [Column("quantity_changed")]
        [Required]
        public int QuantityChanged { get; set; }

        /// <summary>
        /// Stock level before this movement
        /// </summary>
        [Column("previous_quantity")]
        [Required]
        public int PreviousQuantity { get; set; }

        /// <summary>
        /// Stock level after this movement
        /// </summary>
        [Column("new_quantity")]
        [Required]
        public int NewQuantity { get; set; }

        /// <summary>
        /// Type of inventory movement
        /// </summary>
        [Column("movement_type")]
        [StringLength(50)]
        [Required]
        public string MovementType { get; set; } = string.Empty;

        /// <summary>
        /// Work order that triggered this movement (if applicable)
        /// </summary>
        [Column("work_order_id")]
        public int? WorkOrderId { get; set; }

        /// <summary>
        /// WorkOrderPart record that triggered this consumption (if applicable)
        /// </summary>
        [Column("work_order_part_id")]
        public int? WorkOrderPartId { get; set; }

        /// <summary>
        /// User who performed this movement
        /// </summary>
        [Column("performed_by_user_id")]
        [StringLength(450)]
        [Required]
        public string PerformedByUserId { get; set; } = string.Empty;

        /// <summary>
        /// Unit cost at time of movement (for cost tracking)
        /// </summary>
        [Column("unit_cost_snapshot", TypeName = "decimal(10,2)")]
        public decimal? UnitCostSnapshot { get; set; }

        /// <summary>
        /// Total cost of this movement (quantity * unit cost)
        /// </summary>
        [Column("total_cost", TypeName = "decimal(10,2)")]
        public decimal? TotalCost { get; set; }

        /// <summary>
        /// Additional notes or reason for movement
        /// </summary>
        [Column("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Immutable timestamp - when this movement occurred
        /// </summary>
        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("PartId")]
        public virtual Part? Part { get; set; }

        [ForeignKey("WorkOrderId")]
        public virtual WorkOrder? WorkOrder { get; set; }

        [ForeignKey("WorkOrderPartId")]
        public virtual WorkOrderPart? WorkOrderPart { get; set; }

        [ForeignKey("PerformedByUserId")]
        public virtual ApplicationUser? PerformedByUser { get; set; }
    }

    /// <summary>
    /// Inventory movement types
    /// Defines all possible inventory mutation operations
    /// </summary>
    public static class InventoryMovementTypes
    {
        /// <summary>
        /// Parts consumed during work order completion
        /// </summary>
        public const string Consumption = "Consumption";

        /// <summary>
        /// Manual stock adjustment (increase or decrease)
        /// </summary>
        public const string Adjustment = "Adjustment";

        /// <summary>
        /// New stock received from supplier
        /// </summary>
        public const string Restock = "Restock";

        /// <summary>
        /// Parts returned from work order (unused)
        /// </summary>
        public const string Return = "Return";

        /// <summary>
        /// Correction of previous error
        /// </summary>
        public const string Correction = "Correction";

        /// <summary>
        /// Initial stock entry
        /// </summary>
        public const string InitialStock = "InitialStock";

        /// <summary>
        /// Stock transfer between locations
        /// </summary>
        public const string Transfer = "Transfer";

        /// <summary>
        /// Get all valid movement types
        /// </summary>
        public static string[] GetAll() => new[]
        {
            Consumption,
            Adjustment,
            Restock,
            Return,
            Correction,
            InitialStock,
            Transfer
        };

        /// <summary>
        /// Validate if movement type is valid
        /// </summary>
        public static bool IsValid(string movementType)
        {
            return GetAll().Contains(movementType);
        }
    }
}
