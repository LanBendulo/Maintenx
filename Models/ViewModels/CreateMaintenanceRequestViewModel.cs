using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace IT15_Project.Models.ViewModels
{
    /// <summary>
    /// ViewModel for creating a new maintenance request
    /// </summary>
    public class CreateMaintenanceRequestViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an asset")]
        [Display(Name = "Asset")]
        public int AssetId { get; set; }

        [Required(ErrorMessage = "Please select a priority")]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Medium";

        [StringLength(50)]
        [Display(Name = "Category")]
        public string? Category { get; set; }

        [StringLength(200)]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Display(Name = "Attachment")]
        public IFormFile? Attachment { get; set; }
    }
}
