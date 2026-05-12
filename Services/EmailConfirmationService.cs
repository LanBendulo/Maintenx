using IT15_Project.Models;
using IT15_Project.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace IT15_Project.Services
{
    /// <summary>
    /// Email Confirmation Service Implementation
    /// Generates confirmation tokens and sends branded confirmation emails
    /// </summary>
    public class EmailConfirmationService : IEmailConfirmationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<EmailConfirmationService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailConfirmationService(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IEmailTemplateService templateService,
            ILogger<EmailConfirmationService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _emailService = emailService;
            _templateService = templateService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Send email confirmation link to user
        /// </summary>
        public async Task<bool> SendConfirmationEmailAsync(ApplicationUser user, string? returnUrl = null)
        {
            try
            {
                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Encode token for URL
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                // Build confirmation URL
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogError("HttpContext is null - cannot generate confirmation URL");
                    return false;
                }

                var request = httpContext.Request;
                var scheme = request.Scheme;
                var host = request.Host.ToString();

                var confirmationUrl = $"{scheme}://{host}/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    confirmationUrl += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
                }

                // Send confirmation email using template
                var emailBody = await _templateService.RenderConfirmEmailAsync(
                    user.UserName ?? user.Email ?? "User",
                    confirmationUrl
                );

                await _emailService.SendAsync(user.Email!, "Confirm Your MaintenX Email", emailBody);

                _logger.LogInformation("Email confirmation sent to: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email to: {Email}", user.Email);
                return false;
            }
        }
    }
}
