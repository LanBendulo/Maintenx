using System.ComponentModel.DataAnnotations;

namespace IT15_Project.Models.ViewModels
{
    /// <summary>
    /// ViewModel for creating a new work order
    /// Can be created from a Maintenance Request (conversion) or manually
    /// </summary>
    public class CreateWorkOrderViewModel : IValidatableObject
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

        /// <summary>
        /// Custom validation for date fields
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Validate Start Date is not in the past
            if (DateCreated.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Start date cannot be in the past.",
                    new[] { nameof(DateCreated) }
                );
            }

            // Validate Expected Completion is after Start Date
            if (DueDate.Date <= DateCreated.Date)
            {
                yield return new ValidationResult(
                    "Expected completion must be after the start date.",
                    new[] { nameof(DueDate) }
                );
            }

            // Validate duration is not more than 365 days
            var duration = (DueDate.Date - DateCreated.Date).Days;
            if (duration > 365)
            {
                yield return new ValidationResult(
                    "Schedule duration cannot exceed 365 days.",
                    new[] { nameof(DueDate) }
                );
            }
        }
    }
}
