using IT15_Project.Configuration;
using IT15_Project.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace IT15_Project.Services
{
    /// <summary>
    /// Email Service Implementation using MailKit
    /// Provides production-ready SMTP email functionality
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Send an HTML email asynchronously using MailKit
        /// </summary>
        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            try
            {
                // Validate configuration
                if (string.IsNullOrEmpty(_emailSettings.SmtpHost))
                {
                    _logger.LogError("SMTP configuration is missing. Email cannot be sent.");
                    throw new InvalidOperationException("SMTP host is not configured.");
                }

                if (string.IsNullOrEmpty(_emailSettings.SenderEmail))
                {
                    _logger.LogError("Sender email is not configured.");
                    throw new InvalidOperationException("Sender email is not configured.");
                }

                // Build email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                // Create HTML body
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                // Send email via SMTP
                using var client = new SmtpClient();
                
                // Connect to SMTP server
                await client.ConnectAsync(
                    _emailSettings.SmtpHost,
                    _emailSettings.SmtpPort,
                    _emailSettings.UseSSL ? SecureSocketOptions.StartTls : SecureSocketOptions.None
                );

                // Authenticate if credentials provided
                if (!string.IsNullOrEmpty(_emailSettings.Username) && !string.IsNullOrEmpty(_emailSettings.Password))
                {
                    await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                }

                // Send message
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Recipient} with subject: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient} with subject: {Subject}", to, subject);
                throw;
            }
        }
    }
}
