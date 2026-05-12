using IT15_Project.Models;
using IT15_Project.Models.ViewModels;
using IT15_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// Account Controller
    /// Handles: Forgot Password, Reset Password, Email Verification
    /// SECURITY: Generic responses to prevent email enumeration
    /// </summary>
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IEmailTemplateService templateService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _templateService = templateService;
            _logger = logger;
        }

        // ========================================
        // FORGOT PASSWORD
        // ========================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);

            // SECURITY: Always return success message to prevent email enumeration
            // Do NOT reveal whether the email exists in the system
            if (user == null)
            {
                _logger.LogInformation("Password reset requested for non-existent email: {Email}", model.Email);
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // Encode token for URL
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Build reset URL
            var resetUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { token = encodedToken, email = model.Email },
                protocol: Request.Scheme
            );

            if (resetUrl == null)
            {
                _logger.LogError("Failed to generate reset URL for user: {Email}", model.Email);
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // Send password reset email using template
            try
            {
                var emailBody = await _templateService.RenderForgotPasswordAsync(user.UserName ?? "User", resetUrl);
                await _emailService.SendAsync(model.Email, "Reset Your MaintenX Password", emailBody);
                
                _logger.LogInformation("Password reset email sent to: {Email}", model.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to: {Email}", model.Email);
                // Still return success to prevent email enumeration
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // ========================================
        // RESET PASSWORD
        // ========================================

        [HttpGet]
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Reset password accessed with missing token or email");
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ForgotPasswordResetViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ForgotPasswordResetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // SECURITY: Don't reveal that the user doesn't exist
                _logger.LogWarning("Reset password attempted for non-existent email: {Email}", model.Email);
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            // Decode token
            string decodedToken;
            try
            {
                var tokenBytes = WebEncoders.Base64UrlDecode(model.Token);
                decodedToken = Encoding.UTF8.GetString(tokenBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode reset token for user: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Invalid or expired reset link.");
                return View(model);
            }

            // Reset password using Identity
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password successfully reset for user: {Email}", model.Email);
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            // Add errors to model state
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                _logger.LogWarning("Password reset failed for {Email}: {Error}", model.Email, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // ========================================
        // EMAIL VERIFICATION (Infrastructure Only)
        // ========================================

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Email confirmation accessed with missing userId or token");
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Email confirmation attempted for non-existent user: {UserId}", userId);
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            // Decode token
            string decodedToken;
            try
            {
                var tokenBytes = WebEncoders.Base64UrlDecode(token);
                decodedToken = Encoding.UTF8.GetString(tokenBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode email confirmation token for user: {UserId}", userId);
                return BadRequest("Invalid confirmation link.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed for user: {UserId}", userId);
                ViewBag.Message = "Thank you for confirming your email.";
                ViewBag.Success = true;
            }
            else
            {
                _logger.LogWarning("Email confirmation failed for user: {UserId}", userId);
                ViewBag.Message = "Error confirming your email.";
                ViewBag.Success = false;
            }

            return View();
        }

        // ========================================
        // SEND CONFIRMATION EMAIL (Helper Method)
        // ========================================

        /// <summary>
        /// Send email confirmation link to user
        /// Called after registration or when resending confirmation
        /// </summary>
        public async Task<bool> SendConfirmationEmailAsync(ApplicationUser user)
        {
            try
            {
                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Encode token for URL
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                // Build confirmation URL
                var confirmationUrl = Url.Action(
                    nameof(ConfirmEmail),
                    "Account",
                    new { userId = user.Id, token = encodedToken },
                    protocol: Request.Scheme
                );

                if (confirmationUrl == null)
                {
                    _logger.LogError("Failed to generate confirmation URL for user: {UserId}", user.Id);
                    return false;
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
