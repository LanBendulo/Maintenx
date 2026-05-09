using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Extended Identity User for authentication and authorization
    /// Multi-tenant: All users belong to a Company (except SuperAdmin)
    /// Domain-specific personnel data is in Personnel table
    /// Relationship: Personnel → User (one-directional, Personnel owns the FK)
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Company (Tenant) ID - Required for tenant users, null for SuperAdmin
        /// SuperAdmin (CompanyId = null) operates at platform level
        /// Tenant users (CompanyId != null) are scoped to their company
        /// </summary>
        [Column("CompanyId")]
        public int? CompanyId { get; set; }

        /// <summary>
        /// Full name of the user (optional)
        /// </summary>
        [Column("FullName")]
        [StringLength(200)]
        public string? FullName { get; set; }

        /// <summary>
        /// Account active status (for soft deactivation)
        /// Inactive users cannot log in
        /// </summary>
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last successful login timestamp
        /// Updated automatically on login
        /// </summary>
        [Column("LastLoginAt")]
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Account creation timestamp
        /// </summary>
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Last account update timestamp
        /// </summary>
        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        /// <summary>
        /// Optional link to Personnel record (if user is operational staff)
        /// Relationship owned by Personnel table via Personnel.UserId FK
        /// This is a navigation property only - no FK column here
        /// </summary>
        public virtual Personnel? Personnel { get; set; }
    }
}
