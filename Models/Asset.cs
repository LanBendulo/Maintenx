using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents an equipment or asset
    /// </summary>
    [Table("Asset")]
    public class Asset
    {
        [Key]
        [Column("asset_id")]
        public int AssetId { get; set; }

        [Column("asset_name")]
        [StringLength(100)]
        [Required]
        public string AssetName { get; set; } = string.Empty;

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("location")]
        [StringLength(150)]
        public string? Location { get; set; }

        [Column("status")]
        [StringLength(30)]
        public string? Status { get; set; }

        [Column("purchase_date")]
        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
    }
}
