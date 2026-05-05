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

        [Column("company_id")]
        [Required]
        public int CompanyId { get; set; }

        [Column("asset_name")]
        [StringLength(100)]
        [Required]
        public string AssetName { get; set; } = string.Empty;

        [Column("asset_code")]
        [StringLength(50)]
        public string? AssetCode { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("location")]
        [StringLength(150)]
        public string? Location { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("status")]
        [StringLength(30)]
        public string? Status { get; set; }

        [Column("purchase_date")]
        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
    }
}
