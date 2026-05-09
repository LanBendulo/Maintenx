// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

namespace IT15_Project.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser>  _userManager;
        private readonly ILogger<LoginModel>        _logger;
        private readonly ApplicationDbContext _context;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser>  userManager,
            ILogger<LoginModel>        logger,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager   = userManager;
            _logger        = logger;
            _context       = context;
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

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
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

                        // DEBUG: Get user roles
                        var userRoles = await _userManager.GetRolesAsync(user);
                        _logger.LogInformation("User roles: {Roles}", string.Join(", ", userRoles));

                        // SuperAdmin goes to platform dashboard (SaaS management)
                        if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
                        {
                            _logger.LogInformation("Redirecting SuperAdmin to /superadmin/dashboard");
                            return Redirect("/superadmin/dashboard");
                        }

                        // Check if company is active and subscription is valid (tenant users only)
                        if (user.CompanyId.HasValue)
                        {
                            var company = await _signInManager.UserManager.Users
                                .Where(u => u.Id == user.Id)
                                .Select(u => u.Company)
                                .FirstOrDefaultAsync();

                            if (company != null)
                            {
                                // Check if company is suspended
                                if (!company.IsActive)
                                {
                                    _logger.LogWarning("Login blocked: Company is suspended for user: {Email}", Input.Email);
                                    await _signInManager.SignOutAsync();
                                    ModelState.AddModelError(string.Empty, "Your company account has been suspended. Please contact support.");
                                    return Page();
                                }

                                // Check if subscription is expired
                                if (company.SubscriptionExpiry.HasValue && company.SubscriptionExpiry.Value < DateTime.Now)
                                {
                                    _logger.LogWarning("Login blocked: Subscription expired for user: {Email}", Input.Email);
                                    await _signInManager.SignOutAsync();
                                    ModelState.AddModelError(string.Empty, "Your company subscription has expired. Please contact your administrator.");
                                    return Page();
                                }
                            }
                        }

                        // Owner and Admin go to admin dashboard (full system metrics)
                        if (await _userManager.IsInRoleAsync(user, "Admin") || 
                            await _userManager.IsInRoleAsync(user, "Owner"))
                        {
                            _logger.LogInformation("Redirecting Owner/Admin to /admin/dashboard");
                            return Redirect("/admin/dashboard");
                        }

                        // Technician goes to dedicated technician dashboard
                        if (await _userManager.IsInRoleAsync(user, "Technician"))
                        {
                            _logger.LogInformation("Redirecting Technician to /dashboard");
                            return Redirect("/dashboard");
                        }

                        // User goes to user dashboard (maintenance requests)
                        if (await _userManager.IsInRoleAsync(user, "User"))
                        {
                            _logger.LogInformation("Redirecting User to /userdashboard");
                            return Redirect("/userdashboard");
                        }

                        // DEBUG: No role matched
                        _logger.LogWarning("No role matched for user: {Email}, Roles: {Roles}", Input.Email, string.Join(", ", userRoles));
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
    }
}
