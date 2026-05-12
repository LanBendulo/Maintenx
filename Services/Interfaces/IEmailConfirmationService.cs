using IT15_Project.Models;

namespace IT15_Project.Services.Interfaces
{
    /// <summary>
    /// Email Confirmation Service Interface
    /// Handles sending email confirmation links to users
    /// </summary>
    public interface IEmailConfirmationService
    {
        /// <summary>
        /// Send email confirmation link to user
        /// </summary>
        /// <param name="user">User to send confirmation to</param>
        /// <param name="returnUrl">Optional return URL after confirmation</param>
        /// <returns>True if email sent successfully</returns>
        Task<bool> SendConfirmationEmailAsync(ApplicationUser user, string? returnUrl = null);
    }
}
