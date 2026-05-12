namespace IT15_Project.Services.Interfaces
{
    /// <summary>
    /// Email Template Service Interface
    /// Provides reusable HTML email template rendering with placeholder replacement
    /// </summary>
    public interface IEmailTemplateService
    {
        /// <summary>
        /// Render an email template with placeholder replacement
        /// </summary>
        /// <param name="templateName">Template file name (without .html extension)</param>
        /// <param name="placeholders">Dictionary of placeholder keys and values</param>
        /// <returns>Rendered HTML email content</returns>
        Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> placeholders);

        /// <summary>
        /// Render email confirmation template
        /// </summary>
        Task<string> RenderConfirmEmailAsync(string userName, string confirmationUrl);

        /// <summary>
        /// Render forgot password template
        /// </summary>
        Task<string> RenderForgotPasswordAsync(string userName, string resetUrl);
    }
}
