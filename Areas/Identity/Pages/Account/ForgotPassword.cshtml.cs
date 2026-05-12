// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using IT15_Project.Models;
using IT15_Project.Services.Security;
using Microsoft.AspNetCore.RateLimiting;

namespace IT15_Project.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ITurnstileValidationService _turnstileValidationService;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager, 
            IEmailSender emailSender,
            ITurnstileValidationService turnstileValidationService,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _turnstileValidationService = turnstileValidationService;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     Turnstile CAPTCHA token
            ///     Only required when Turnstile is enabled
            /// </summary>
            public string TurnstileToken { get; set; }
        }

        [EnableRateLimiting("forgotPassword")]
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // ============================================================
                // Validate Turnstile CAPTCHA (if enabled)
                // ============================================================
                if (_turnstileValidationService.IsEnabled())
                {
                    // Validate token is provided
                    if (string.IsNullOrWhiteSpace(Input.TurnstileToken))
                    {
                        _logger.LogWarning("Turnstile token missing for forgot password attempt. Email: {Email}", Input.Email);
                        ModelState.AddModelError(string.Empty, "Please complete the CAPTCHA verification.");
                        return Page();
                    }

                    var turnstileValid = await _turnstileValidationService.ValidateTokenAsync(
                        Input.TurnstileToken,
                        HttpContext.Connection.RemoteIpAddress?.ToString());

                    if (!turnstileValid)
                    {
                        _logger.LogWarning("Forgot password attempt failed Turnstile validation from IP: {IP}", 
                            HttpContext.Connection.RemoteIpAddress);
                        ModelState.AddModelError(string.Empty, "CAPTCHA validation failed. Please try again.");
                        return Page();
                    }

                    _logger.LogInformation("Turnstile validation succeeded for forgot password");
                }
                else
                {
                    _logger.LogInformation("Turnstile is disabled - skipping CAPTCHA validation for forgot password");
                }
                // ============================================================

                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Reset Password",
                    $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
