// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Models;
using IT15_Project.Data;
using IT15_Project.Services.Security;
using Microsoft.AspNetCore.RateLimiting;

namespace IT15_Project.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser>  _userManager;
        private readonly ILogger<LoginModel>        _logger;
        private readonly ApplicationDbContext _context;
        private readonly ITurnstileValidationService _turnstileService;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser>  userManager,
            ILogger<LoginModel>        logger,
            ApplicationDbContext context,
            ITurnstileValidationService turnstileService)
        {
            _signInManager = signInManager;
            _userManager   = userManager;
            _logger        = logger;
            _context       = context;
            _turnstileService = turnstileService;
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
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

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
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }

            /// <summary>
            /// Cloudflare Turnstile response token for CAPTCHA validation
            /// Only required when Turnstile is enabled
            /// </summary>
            public string TurnstileToken { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // DEBUG: Log external authentication schemes
            _logger.LogInformation("=== External Authentication Schemes ===");
            _logger.LogInformation("Count: {Count}", ExternalLogins.Count);
            foreach (var scheme in ExternalLogins)
            {
                _logger.LogInformation("Provider: {Name}, DisplayName: {DisplayName}", scheme.Name, scheme.DisplayName);
            }

            ReturnUrl = returnUrl;
        }

        [EnableRateLimiting("login")]
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // ═══════════════════════════════════════════════════════════
                // TURNSTILE CAPTCHA VALIDATION (Server-Side)
                // Only validate if Turnstile is enabled in configuration
                // ═══════════════════════════════════════════════════════════
                if (_turnstileService.IsEnabled())
                {
                    // Validate token is provided
                    if (string.IsNullOrWhiteSpace(Input.TurnstileToken))
                    {
                        _logger.LogWarning("Turnstile token missing for login attempt. Email: {Email}", Input.Email);
                        ModelState.AddModelError(string.Empty, "Please complete the CAPTCHA verification.");
                        return Page();
                    }

                    var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var turnstileResult = await _turnstileService.ValidateTokenDetailedAsync(
                        Input.TurnstileToken, remoteIp);

                    if (!turnstileResult.Success)
                    {
                        _logger.LogWarning(
                            "Turnstile validation failed for login attempt. Email: {Email}, IP: {IP}, Errors: {Errors}",
                            Input.Email, remoteIp, string.Join(", ", turnstileResult.ErrorCodes));

                        ModelState.AddModelError(string.Empty, 
                            turnstileResult.ErrorMessage ?? "CAPTCHA verification failed. Please try again.");
                        return Page();
                    }

                    _logger.LogInformation("Turnstile validation succeeded for login attempt: {Email}", Input.Email);
                }
                else
                {
                    _logger.LogInformation("Turnstile is disabled - skipping CAPTCHA validation for login attempt: {Email}", Input.Email);
                }
                // ═══════════════════════════════════════════════════════════

                // DEBUG: Log login attempt
                _logger.LogInformation("Login attempt for email: {Email}", Input.Email);

                // Check if user exists
                var userCheck = await _userManager.FindByEmailAsync(Input.Email);
                if (userCheck == null)
                {
                    _logger.LogWarning("Login failed: User not found for email: {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }

                // DEBUG: Log user details
                _logger.LogInformation("User found: Id={UserId}, IsActive={IsActive}, EmailConfirmed={EmailConfirmed}, LockoutEnd={LockoutEnd}", 
                    userCheck.Id, userCheck.IsActive, userCheck.EmailConfirmed, userCheck.LockoutEnd);

                // Check if user is active
                if (!userCheck.IsActive)
                {
                    _logger.LogWarning("Login failed: User account is inactive for email: {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact your administrator.");
                    return Page();
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                
                // DEBUG: Log sign-in result
                _logger.LogInformation("SignIn result: Succeeded={Succeeded}, IsLockedOut={IsLockedOut}, IsNotAllowed={IsNotAllowed}, RequiresTwoFactor={RequiresTwoFactor}", 
                    result.Succeeded, result.IsLockedOut, result.IsNotAllowed, result.RequiresTwoFactor);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in successfully.");

                    // ── Role-based redirect ──────────────────────────────
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        // Update LastLoginAt timestamp
                        user.LastLoginAt = DateTime.Now;
                        await _context.SaveChangesAsync();

                        // Use centralized role-based redirect
                        return await RedirectBasedOnRole(user, returnUrl);
                    }

                    // Fallback: go to home page
                    _logger.LogInformation("Fallback redirect to: {ReturnUrl}", returnUrl);
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        // ============================================================
        // EXTERNAL LOGIN (Google Sign-In)
        // ============================================================

        /// <summary>
        /// Initiates external authentication (Google)
        /// </summary>
        public IActionResult OnPostExternalLogin(string provider, string returnUrl = null)
        {
            _logger.LogInformation("=== OnPostExternalLogin called ===");
            _logger.LogInformation("Provider: {Provider}", provider);
            _logger.LogInformation("ReturnUrl: {ReturnUrl}", returnUrl);

            // Request a redirect to the external login provider
            var redirectUrl = Url.Page("./Login", pageHandler: "ExternalLoginCallback", values: new { returnUrl });
            _logger.LogInformation("RedirectUrl: {RedirectUrl}", redirectUrl);

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            _logger.LogInformation("Challenging external provider: {Provider}", provider);

            return new ChallengeResult(provider, properties);
        }

        /// <summary>
        /// Callback handler after external authentication
        /// Creates or links Identity account based on external email
        /// </summary>
        public async Task<IActionResult> OnGetExternalLoginCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            _logger.LogInformation("=== OnGetExternalLoginCallbackAsync called ===");
            _logger.LogInformation("ReturnUrl: {ReturnUrl}", returnUrl);
            _logger.LogInformation("RemoteError: {RemoteError}", remoteError);

            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                _logger.LogError("External provider error: {Error}", remoteError);
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Get external login info from the authentication cookie
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                
                // Update last login timestamp
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null)
                {
                    user.LastLoginAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Role-based redirect (same logic as local login)
                    return await RedirectBasedOnRole(user, returnUrl);
                }

                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                // User doesn't have an account yet - create one
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(email))
                {
                    ErrorMessage = "Email not provided by external provider.";
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }

                // Check if user already exists with this email (link accounts)
                var existingUser = await _userManager.FindByEmailAsync(email);

                if (existingUser != null)
                {
                    // Link external login to existing account
                    var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                    if (addLoginResult.Succeeded)
                    {
                        _logger.LogInformation("External login linked to existing account: {Email}", email);
                        await _signInManager.SignInAsync(existingUser, isPersistent: false);
                        
                        // Update last login timestamp
                        existingUser.LastLoginAt = DateTime.Now;
                        await _context.SaveChangesAsync();

                        return await RedirectBasedOnRole(existingUser, returnUrl);
                    }
                    else
                    {
                        ErrorMessage = "Failed to link external login to existing account.";
                        return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                    }
                }

                // Create new user account
                // IMPORTANT: Google users default to lowest role (User)
                // IMPORTANT: No CompanyId assigned - must be set by admin later
                var newUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true, // Google email is already verified
                    FullName = info.Principal.FindFirstValue(ClaimTypes.Name),
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CompanyId = null // No company assigned - admin must assign later
                };

                var createResult = await _userManager.CreateAsync(newUser);
                if (createResult.Succeeded)
                {
                    // Add external login
                    createResult = await _userManager.AddLoginAsync(newUser, info);
                    if (createResult.Succeeded)
                    {
                        // Assign default role: User (lowest privilege)
                        // SECURITY: Do NOT auto-assign Admin or Owner roles
                        await _userManager.AddToRoleAsync(newUser, "User");

                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        await _signInManager.SignInAsync(newUser, isPersistent: false, info.LoginProvider);
                        
                        // Update last login timestamp
                        newUser.LastLoginAt = DateTime.Now;
                        await _context.SaveChangesAsync();

                        // Redirect to user dashboard (default for new Google users)
                        return Redirect("/userdashboard");
                    }
                }

                // If we got here, account creation failed
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }
        }

        /// <summary>
        /// Role-based redirect helper (reusable for local and external login)
        /// </summary>
        private async Task<IActionResult> RedirectBasedOnRole(ApplicationUser user, string returnUrl)
        {
            // SuperAdmin goes to platform dashboard
            if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                return Redirect("/superadmin/dashboard");
            }

            // Check company status for tenant users
            if (user.CompanyId.HasValue)
            {
                var company = await _context.Companies.FindAsync(user.CompanyId.Value);
                if (company != null)
                {
                    if (!company.IsActive)
                    {
                        await _signInManager.SignOutAsync();
                        ErrorMessage = "Your company account has been suspended. Please contact support.";
                        return RedirectToPage("./Login");
                    }

                    if (company.SubscriptionExpiry.HasValue && company.SubscriptionExpiry.Value < DateTime.Now)
                    {
                        await _signInManager.SignOutAsync();
                        ErrorMessage = "Your company subscription has expired. Please contact your administrator.";
                        return RedirectToPage("./Login");
                    }
                }
            }

            // Owner and Admin go to admin dashboard
            if (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "Owner"))
            {
                return Redirect("/admin/dashboard");
            }

            // Supervisor goes to supervisor dashboard
            if (await _userManager.IsInRoleAsync(user, "Supervisor"))
            {
                return Redirect("/supervisor/dashboard");
            }

            // Technician goes to technician dashboard
            if (await _userManager.IsInRoleAsync(user, "Technician"))
            {
                return Redirect("/dashboard");
            }

            // User goes to user dashboard
            if (await _userManager.IsInRoleAsync(user, "User"))
            {
                return Redirect("/userdashboard");
            }

            // Fallback
            return LocalRedirect(returnUrl);
        }
    }
}
