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

        [Column("category_name")]
        [StringLength(100)]
        [Required]
        public string CategoryName { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }
}
