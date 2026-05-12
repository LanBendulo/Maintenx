using IT15_Project.Services.Interfaces;

namespace IT15_Project.Services
{
    /// <summary>
    /// Email Template Service Implementation
    /// Loads HTML templates from disk and performs placeholder replacement
    /// </summary>
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EmailTemplateService> _logger;
        private const string TemplateFolder = "Templates/Emails";

        public EmailTemplateService(IWebHostEnvironment environment, ILogger<EmailTemplateService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Render an email template with placeholder replacement
        /// </summary>
        public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> placeholders)
        {
            try
            {
                // Load template content
                var templatePath = Path.Combine(_environment.ContentRootPath, TemplateFolder, $"{templateName}.html");
                
                if (!File.Exists(templatePath))
                {
                    _logger.LogError("Email template not found: {TemplatePath}", templatePath);
                    throw new FileNotFoundException($"Email template '{templateName}' not found.");
                }

                var templateContent = await File.ReadAllTextAsync(templatePath);

                // Load layout if exists
                var layoutPath = Path.Combine(_environment.ContentRootPath, TemplateFolder, "Layout.html");
                string finalHtml;

                if (File.Exists(layoutPath))
                {
                    var layoutContent = await File.ReadAllTextAsync(layoutPath);
                    
                    // Replace {{Content}} in layout with template content
                    finalHtml = layoutContent.Replace("{{Content}}", templateContent);
                }
                else
                {
                    // No layout, use template directly
                    finalHtml = templateContent;
                }

                // Replace all placeholders
                foreach (var placeholder in placeholders)
                {
                    finalHtml = finalHtml.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
                }

                // Replace common placeholders
                finalHtml = finalHtml.Replace("{{AppName}}", "MaintenX");
                finalHtml = finalHtml.Replace("{{CurrentYear}}", DateTime.Now.Year.ToString());

                _logger.LogInformation("Email template '{TemplateName}' rendered successfully", templateName);
                return finalHtml;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render email template: {TemplateName}", templateName);
                throw;
            }
        }

        /// <summary>
        /// Render email confirmation template
        /// </summary>
        public async Task<string> RenderConfirmEmailAsync(string userName, string confirmationUrl)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "UserName", userName },
                { "ActionUrl", confirmationUrl },
                { "Subject", "Confirm Your Email" }
            };

            return await RenderTemplateAsync("ConfirmEmail", placeholders);
        }

        /// <summary>
        /// Render forgot password template
        /// </summary>
        public async Task<string> RenderForgotPasswordAsync(string userName, string resetUrl)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "UserName", userName },
                { "ActionUrl", resetUrl },
                { "Subject", "Reset Your Password" }
            };

            return await RenderTemplateAsync("ForgotPassword", placeholders);
        }
    }
}
