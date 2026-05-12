// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Services.Interfaces;
using IT15_Project.Services.Security;

namespace IT15_Project.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailConfirmationService _emailConfirmationService;
        private readonly ApplicationDbContext _context;
        private readonly ITurnstileValidationService _turnstileValidationService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailConfirmationService emailConfirmationService,
            ApplicationDbContext context,
            ITurnstileValidationService turnstileValidationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailConfirmationService = emailConfirmationService;
            _context = context;
            _turnstileValidationService = turnstileValidationService;
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
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     Multi-tenant registration input model
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     Company name for multi-tenant isolation
            /// </summary>
            [Required]
            [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
            [Display(Name = "Company Name")]
            public string CompanyName { get; set; }

            /// <summary>
            ///     User's full name
            /// </summary>
            [Required]
            [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            /// <summary>
            ///     Email address (will be username)
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     Password
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     Confirm password
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            ///     Turnstile CAPTCHA token
            ///     Only required when Turnstile is enabled
            /// </summary>
            public string TurnstileToken { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        [EnableRateLimiting("registration")]
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            
            if (ModelState.IsValid)
            {
                // ============================================================
                // PHASE 0: Validate Turnstile CAPTCHA (if enabled)
                // ============================================================
                if (_turnstileValidationService.IsEnabled())
                {
                    // Validate token is provided
                    if (string.IsNullOrWhiteSpace(Input.TurnstileToken))
                    {
                        _logger.LogWarning("Turnstile token missing for registration attempt. Email: {Email}", Input.Email);
                        ModelState.AddModelError(string.Empty, "Please complete the CAPTCHA verification.");
                        return Page();
                    }

                    var turnstileValid = await _turnstileValidationService.ValidateTokenAsync(
                        Input.TurnstileToken,
                        HttpContext.Connection.RemoteIpAddress?.ToString());

                    if (!turnstileValid)
                    {
                        _logger.LogWarning("Registration attempt failed Turnstile validation from IP: {IP}", 
                            HttpContext.Connection.RemoteIpAddress);
                        ModelState.AddModelError(string.Empty, "CAPTCHA validation failed. Please try again.");
                        return Page();
                    }

                    _logger.LogInformation("Turnstile validation succeeded for registration");
                }
                else
                {
                    _logger.LogInformation("Turnstile is disabled - skipping CAPTCHA validation for registration");
                }
                // ============================================================
                // ============================================================
                // PHASE 1: Ensure Required Roles Exist
                // ============================================================
                await EnsureRolesExistAsync();

                // ============================================================
                // PHASE 2: Create Company (Tenant) FIRST
                // ============================================================
                Company company = null;
                try
                {
                    company = new Company
                    {
                        CompanyName = Input.CompanyName.Trim(),
                        SubscriptionPlan = "Free", // Default plan for new registrations
                        SubscriptionExpiry = DateTime.UtcNow.AddMonths(1), // 1 month free trial
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        ContactEmail = Input.Email,
                        MaxUsers = 5,  // Free plan limit
                        MaxAssets = 50 // Free plan limit
                    };

                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Company '{company.CompanyName}' created with ID: {company.CompanyId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create company during registration");
                    ModelState.AddModelError(string.Empty, "Failed to create company. Please try again.");
                    return Page();
                }

                // ============================================================
                // PHASE 3: Create ApplicationUser with CompanyId
                // ============================================================
                var user = CreateUser();
                user.CompanyId = company.CompanyId; // Link user to company
                user.FullName = Input.FullName.Trim();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User '{Input.Email}' created successfully for Company ID: {company.CompanyId}");

                    // ============================================================
                    // PHASE 4: Assign Owner Role
                    // ============================================================
                    try
                    {
                        await _userManager.AddToRoleAsync(user, "Owner");
                        _logger.LogInformation($"User '{Input.Email}' assigned to 'Owner' role");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to assign Owner role to user");
                        // Continue anyway - user is created, role can be assigned later
                    }

                    // ============================================================
                    // PHASE 5: Email Confirmation using branded template service
                    // ============================================================
                    try
                    {
                        await _emailConfirmationService.SendConfirmationEmailAsync(user, returnUrl);
                        _logger.LogInformation($"Confirmation email sent to '{Input.Email}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send confirmation email to '{Email}'", Input.Email);
                        // Continue anyway - email confirmation is optional
                    }

                    // ============================================================
                    // PHASE 6: Sign In and Redirect
                    // ============================================================
                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        // Sign in the user immediately
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation($"User '{Input.Email}' signed in after registration");
                        
                        // Redirect to dashboard
                        return RedirectToAction("Index", "Dashboard", new { area = "" });
                    }
                }
                else
                {
                    // ============================================================
                    // ROLLBACK: User creation failed, delete the company
                    // ============================================================
                    _logger.LogError($"User creation failed for '{Input.Email}'. Rolling back company creation.");
                    
                    try
                    {
                        _context.Companies.Remove(company);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Company '{company.CompanyName}' rolled back successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to rollback company creation");
                    }

                    // Display user creation errors
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        /// <summary>
        /// Ensures that required roles exist in the system
        /// </summary>
        private async Task EnsureRolesExistAsync()
        {
            string[] roles = { "Owner", "Admin", "Technician", "User" };

            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                    _logger.LogInformation($"Role '{roleName}' created");
                }
            }
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
