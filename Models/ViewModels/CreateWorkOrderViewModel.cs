using System.ComponentModel.DataAnnotations;

namespace IT15_Project.Models.ViewModels
{
    /// <summary>
    /// ViewModel for creating a new work order
    /// Can be created from a Maintenance Request (conversion) or manually
    /// </summary>
    public class CreateWorkOrderViewModel
    {
        // Optional: If converting from a maintenance request
        [Display(Name = "Maintenance Request ID")]
        public int? MaintenanceRequestId { get; set; }

        // These fields are required but may be pre-filled from request
        [Required(ErrorMessage = "Please select equipment")]
        [Display(Name = "Equipment / Asset")]
        public int AssetId { get; set; }

        [Required(ErrorMessage = "Please enter an issue description")]
        [Display(Name = "Issue Description")]
        [StringLength(5000)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a priority")]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Medium";

        // These fields are always required for work order creation
        [Required(ErrorMessage = "Please assign a technician")]
        [Display(Name = "Assign Technician")]
        public int AssignedTo { get; set; }

        [Required(ErrorMessage = "Please select a start date")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime DateCreated { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please select a completion date")]
        [Display(Name = "Expected Completion")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);

        [Display(Name = "Instructions / Remarks")]
        [StringLength(2000)]
        public string? Notes { get; set; }
    }
}
