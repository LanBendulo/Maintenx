using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Extended Identity User for authentication
    /// Multi-tenant: All users belong to a Company
    /// Domain-specific personnel data is in Personnel table
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Company (Tenant) ID - Required for multi-tenant isolation
        /// </summary>
        [Column("CompanyId")]
        [Required]
        public int CompanyId { get; set; }

        /// <summary>
        /// Full name of the user (optional)
        /// </summary>
        [Column("FullName")]
        [StringLength(200)]
        public string? FullName { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        // Optional link to Personnel record
        public virtual Personnel? Personnel { get; set; }
    }
}
