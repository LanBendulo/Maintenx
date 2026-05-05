using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_Project.Models
{
    /// <summary>
    /// Extended Identity User for authentication only
    /// Domain-specific personnel data is in Personnel table
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // Optional link to Personnel record
        public virtual Personnel? Personnel { get; set; }
    }
}
