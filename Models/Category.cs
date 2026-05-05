using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Represents an asset category (HVAC, Electrical, etc.)
    /// </summary>
    [Table("Category")]
    public class Category
    {
        [Key]
        [Column("category_id")]
        public int CategoryId { get; set; }

        [Column("company_id")]
        [Required]
        public int CompanyId { get; set; }

        [Column("category_name")]
        [StringLength(100)]
        [Required]
        public string CategoryName { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }
}
