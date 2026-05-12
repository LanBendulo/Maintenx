using System.ComponentModel.DataAnnotations;

namespace IT15_Project.Models.ViewModels
{
    /// <summary>
    /// View Model for Self-Service Password Reset (Forgot Password Flow)
    /// User enters new password after clicking reset link from email
    /// </summary>
    public class ForgotPasswordResetViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";

        [Required]
        public string Token { get; set; } = "";
    }
}
