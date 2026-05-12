namespace IT15_Project.Services.Interfaces
{
    /// <summary>
    /// Email Service Interface
    /// Provides centralized email sending infrastructure for:
    /// - Forgot Password
    /// - Email Verification
    /// - Future: Notifications, PM Reminders, Assignment Alerts
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send an HTML email asynchronously
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject line</param>
        /// <param name="htmlBody">HTML email body content</param>
        /// <returns>Task representing the async operation</returns>
        Task SendAsync(string to, string subject, string htmlBody);
    }
}
