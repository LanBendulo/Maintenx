using System.ComponentModel.DataAnnotations;

namespace IT15_Project.Models.ViewModels
{
    /// <summary>
    /// View Model for Forgot Password Request
    /// User enters email to receive password reset link
    /// </summary>
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = "";
    }
}
