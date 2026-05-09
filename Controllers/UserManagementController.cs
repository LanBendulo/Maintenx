using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Models.ViewModels;
using IT15_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_Project.Controllers
{
    /// <summary>
    /// User Management Controller
    /// Manages system access, accounts, and role assignments
    /// Multi-tenant with proper isolation
    /// </summary>
    [Authorize(Roles = "Owner,Admin,Supervisor")]
    [Route("admin/users")]
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITenantService _tenantService;
        private readonly ILogger<UserManagementController> _logger;

        // Role hierarchy for authorization
        private static readonly Dictionary<string, int> RoleHierarchy = new()
        {
            { "SuperAdmin", 100 },
            { "Owner", 90 },
            { "Admin", 80 },
            { "Supervisor", 70 },
            { "Technician", 60 },
            { "InventoryManager", 60 },
            { "User", 50 }
        };

        public UserManagementController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ITenantService tenantService,
            ILogger<UserManagementController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantService = tenantService;
            _logger = logger;
        }

        /// <summary>
        /// Index - User list with summary metrics
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var currentUser = await _userManager.GetUserAsync(User);
                var currentUserRoles = await _userManager.GetRolesAsync(currentUser!);
                var currentUserRole = currentUserRoles.FirstOrDefault() ?? "User";

                // Get all users for this company
                var users = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.CompanyId == companyId)
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                // Build user list with roles and personnel info
                var userList = new List<UserListItemDto>();
                
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var personnel = await _context.Personnel
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.UserId == user.Id);

                    userList.Add(new UserListItemDto
                    {
                        Id = user.Id,
                        FullName = user.FullName ?? "N/A",
                        Email = user.Email ?? "N/A",
                        Roles = roles.ToList(),
                        IsActive = user.IsActive,
                        EmailConfirmed = user.EmailConfirmed,
                        LastLoginAt = user.LastLoginAt,
                        CreatedAt = user.CreatedAt,
                        HasPersonnelLink = personnel != null,
                        PersonnelId = personnel?.PersonnelId,
                        PersonnelName = personnel != null ? $"{personnel.FirstName} {personnel.LastName}" : null
                    });
                }

                // Calculate metrics
                var viewModel = new UserManagementViewModel
                {
                    TotalUsers = userList.Count,
                    ActiveUsers = userList.Count(u => u.IsActive),
                    TechnicianCount = userList.Count(u => u.Roles.Contains("Technician")),
                    AdminCount = userList.Count(u => u.Roles.Contains("Admin") || u.Roles.Contains("Owner")),
                    Users = userList,
                    AvailableRoles = GetAvailableRolesForUser(currentUserRole)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management index");
                return View("Error");
            }
        }

        /// <summary>
        /// Details - View user account details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                
                var user = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

                if (user == null)
                {
                    return NotFound();
                }

                var roles = await _userManager.GetRolesAsync(user);
                var personnel = await _context.Personnel
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                // Get activity metrics
                var workOrdersCreated = await _context.WorkOrders
                    .CountAsync(w => w.CompanyId == companyId && 
                                     w.CreatedByPersonnel != null && 
                                     w.CreatedByPersonnel.UserId == user.Id);

                var workOrdersAssigned = await _context.WorkOrders
                    .CountAsync(w => w.CompanyId == companyId && 
                                     w.AssignedToPersonnel != null && 
                                     w.AssignedToPersonnel.UserId == user.Id);

                var viewModel = new UserDetailsViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName ?? "N/A",
                    Email = user.Email ?? "N/A",
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    LastLoginAt = user.LastLoginAt,
                    CompanyId = user.CompanyId,
                    CompanyName = user.Company?.CompanyName,
                    Roles = roles.ToList(),
                    HasPersonnelLink = personnel != null,
                    PersonnelId = personnel?.PersonnelId,
                    PersonnelName = personnel != null ? $"{personnel.FirstName} {personnel.LastName}" : null,
                    PersonnelRole = personnel?.Role,
                    PersonnelSkillSet = personnel?.SkillSet,
                    WorkOrdersCreated = workOrdersCreated,
                    WorkOrdersAssigned = workOrdersAssigned
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                return View("Error");
            }
        }

        /// <summary>
        /// Create - Show create user form
        /// </summary>
        [HttpGet("create")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser!);
            var currentUserRole = currentUserRoles.FirstOrDefault() ?? "User";

            var companyId = _tenantService.GetCurrentCompanyId();
            
            // Get available personnel without user accounts
            var availablePersonnel = await _context.Personnel
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId && p.UserId == null)
                .Select(p => new PersonnelOptionDto
                {
                    PersonnelId = p.PersonnelId,
                    FullName = $"{p.FirstName} {p.LastName}",
                    Role = p.Role,
                    HasUserAccount = false
                })
                .ToListAsync();

            var viewModel = new CreateEditUserViewModel
            {
                IsActive = true,
                AvailableRoles = GetAvailableRolesForUser(currentUserRole),
                AvailablePersonnel = availablePersonnel
            };

            return View(viewModel);
        }

        /// <summary>
        /// Create - Process create user form
        /// </summary>
        [HttpPost("create")]
        [Authorize(Roles = "Owner,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var currentUserRoles = await _userManager.GetRolesAsync(currentUser!);
                var currentUserRole = currentUserRoles.FirstOrDefault() ?? "User";

                // Validate role assignment permission
                if (!CanAssignRole(currentUserRole, model.Role))
                {
                    ModelState.AddModelError("", $"You do not have permission to assign the {model.Role} role");
                    return View(model);
                }

                var companyId = _tenantService.GetCurrentCompanyId();

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists");
                    return View(model);
                }

                // Create user
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    CompanyId = companyId,
                    IsActive = model.IsActive,
                    EmailConfirmed = true, // Auto-confirm for admin-created accounts
                    CreatedAt = DateTime.Now
                };

                var password = model.Password ?? GenerateTemporaryPassword();
                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    _logger.LogError("User creation failed for {Email}. Errors: {Errors}", 
                        model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

                _logger.LogInformation("User created successfully: {Email}, Id: {UserId}", user.Email, user.Id);

                // Assign role
                var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Role assignment failed for {Email}. Role: {Role}. Errors: {Errors}", 
                        user.Email, model.Role, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    
                    // Delete the user if role assignment fails
                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError("", $"Failed to assign role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    return View(model);
                }

                _logger.LogInformation("Role assigned successfully: {Email} -> {Role}", user.Email, model.Role);

                // Link to personnel if specified
                if (model.LinkedPersonnelId.HasValue)
                {
                    var personnel = await _context.Personnel.FindAsync(model.LinkedPersonnelId.Value);
                    if (personnel != null && personnel.CompanyId == companyId)
                    {
                        personnel.UserId = user.Id;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Personnel linked: {Email} -> PersonnelId: {PersonnelId}", user.Email, personnel.PersonnelId);
                    }
                }
                else if (model.Role == "Technician" || model.Role == "Supervisor")
                {
                    // Auto-create Personnel record for Technician/Supervisor roles if not linked
                    var nameParts = model.FullName.Split(' ', 2);
                    var firstName = nameParts.Length > 0 ? nameParts[0] : model.FullName;
                    var lastName = nameParts.Length > 1 ? nameParts[1] : "";

                    var personnel = new Personnel
                    {
                        CompanyId = companyId,
                        UserId = user.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Position = model.Role,
                        Role = model.Role,
                        EmploymentType = "FullTime",
                        Status = "Active",
                        HireDate = DateTime.Now,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    _context.Personnel.Add(personnel);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Auto-created Personnel record for {Role}: {Email} -> PersonnelId: {PersonnelId}", 
                        model.Role, user.Email, personnel.PersonnelId);
                }

                _logger.LogInformation("User {Email} created by {AdminEmail}", user.Email, currentUser!.Email);

                TempData["SuccessMessage"] = $"User {user.Email} created successfully. Temporary password: {password}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ModelState.AddModelError("", "An error occurred while creating the user");
                return View(model);
            }
        }

        /// <summary>
        /// Edit - Show edit user form
        /// </summary>
        [HttpGet("{id}/edit")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

                if (user == null)
                {
                    return NotFound();
                }

                var currentUser = await _userManager.GetUserAsync(User);
                var currentUserRoles = await _userManager.GetRolesAsync(currentUser!);
                var currentUserRole = currentUserRoles.FirstOrDefault() ?? "User";

                var userRoles = await _userManager.GetRolesAsync(user);
                var personnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                // Get available personnel
                var availablePersonnel = await _context.Personnel
                    .AsNoTracking()
                    .Where(p => p.CompanyId == companyId && (p.UserId == null || p.UserId == user.Id))
                    .Select(p => new PersonnelOptionDto
                    {
                        PersonnelId = p.PersonnelId,
                        FullName = $"{p.FirstName} {p.LastName}",
                        Role = p.Role,
                        HasUserAccount = p.UserId != null
                    })
                    .ToListAsync();

                var viewModel = new CreateEditUserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName ?? "",
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber,
                    Role = userRoles.FirstOrDefault() ?? "User",
                    IsActive = user.IsActive,
                    LinkedPersonnelId = personnel?.PersonnelId,
                    AvailableRoles = GetAvailableRolesForUser(currentUserRole),
                    AvailablePersonnel = availablePersonnel
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for user {UserId}", id);
                return View("Error");
            }
        }

        /// <summary>
        /// Edit - Process edit user form
        /// </summary>
        [HttpPost("{id}/edit")]
        [Authorize(Roles = "Owner,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, CreateEditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

                if (user == null)
                {
                    return NotFound();
                }

                var currentUser = await _userManager.GetUserAsync(User);
                var currentUserRoles = await _userManager.GetRolesAsync(currentUser!);
                var currentUserRole = currentUserRoles.FirstOrDefault() ?? "User";

                // Prevent self-demotion
                if (user.Id == currentUser!.Id)
                {
                    var userCurrentRoles = await _userManager.GetRolesAsync(user);
                    if (userCurrentRoles.Contains("Owner") || userCurrentRoles.Contains("Admin"))
                    {
                        if (model.Role != "Owner" && model.Role != "Admin")
                        {
                            ModelState.AddModelError("", "You cannot demote yourself from Owner/Admin role");
                            return View(model);
                        }
                    }
                }

                // Validate role assignment permission
                if (!CanAssignRole(currentUserRole, model.Role))
                {
                    ModelState.AddModelError("", $"You do not have permission to assign the {model.Role} role");
                    return View(model);
                }

                // Update user
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                user.IsActive = model.IsActive;
                user.UpdatedAt = DateTime.Now;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

                // Update role if changed
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(model.Role))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                // Update personnel link
                var existingPersonnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);
                
                if (existingPersonnel != null)
                {
                    existingPersonnel.UserId = null; // Unlink
                }

                if (model.LinkedPersonnelId.HasValue)
                {
                    var newPersonnel = await _context.Personnel.FindAsync(model.LinkedPersonnelId.Value);
                    if (newPersonnel != null && newPersonnel.CompanyId == companyId)
                    {
                        newPersonnel.UserId = user.Id;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Email} updated by {AdminEmail}", user.Email, currentUser.Email);

                TempData["SuccessMessage"] = $"User {user.Email} updated successfully";
                return RedirectToAction(nameof(Details), new { id = user.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                ModelState.AddModelError("", "An error occurred while updating the user");
                return View(model);
            }
        }

        /// <summary>
        /// Deactivate - Soft delete user account
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Deactivate(string id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var currentUser = await _userManager.GetUserAsync(User);

                // Prevent self-deactivation
                if (user.Id == currentUser!.Id)
                {
                    return Json(new { success = false, message = "You cannot deactivate your own account" });
                }

                // Prevent deactivating the last Owner
                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Contains("Owner"))
                {
                    var ownerCount = await _context.Users
                        .Where(u => u.CompanyId == companyId && u.IsActive)
                        .CountAsync(u => _context.UserRoles
                            .Any(ur => ur.UserId == u.Id && 
                                      _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Owner")));

                    if (ownerCount <= 1)
                    {
                        return Json(new { success = false, message = "Cannot deactivate the last Owner account" });
                    }
                }

                user.IsActive = false;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Email} deactivated by {AdminEmail}", user.Email, currentUser.Email);

                return Json(new { success = true, message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                return Json(new { success = false, message = "An error occurred while deactivating the user" });
            }
        }

        /// <summary>
        /// Reactivate - Restore deactivated user account
        /// </summary>
        [HttpPost("{id}/reactivate")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Reactivate(string id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.IsActive = true;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                _logger.LogInformation("User {Email} reactivated by {AdminEmail}", user.Email, currentUser!.Email);

                return Json(new { success = true, message = "User reactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating user {UserId}", id);
                return Json(new { success = false, message = "An error occurred while reactivating the user" });
            }
        }

        /// <summary>
        /// ChangeRole - Update user's role
        /// </summary>
        [HttpPost("{id}/change-role")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeRoleViewModel model)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                var currentUserRoles = await _userManager.GetRolesAsync(currentUser!);
                var currentUserRole = currentUserRoles.FirstOrDefault() ?? "User";

                // Prevent self-demotion
                if (user.Id == currentUser.Id)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (userRoles.Contains("Owner") || userRoles.Contains("Admin"))
                    {
                        if (model.NewRole != "Owner" && model.NewRole != "Admin")
                        {
                            return Json(new { success = false, message = "You cannot demote yourself" });
                        }
                    }
                }

                // Validate permission
                if (!CanAssignRole(currentUserRole, model.NewRole))
                {
                    return Json(new { success = false, message = $"You do not have permission to assign the {model.NewRole} role" });
                }

                // Update role
                var existingRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, existingRoles);
                await _userManager.AddToRoleAsync(user, model.NewRole);

                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Email} role changed to {NewRole} by {AdminEmail}", 
                    user.Email, model.NewRole, currentUser.Email);

                return Json(new { success = true, message = $"Role changed to {model.NewRole} successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing role for user {UserId}", id);
                return Json(new { success = false, message = "An error occurred while changing the role" });
            }
        }

        /// <summary>
        /// ResetPassword - Admin-triggered password reset
        /// </summary>
        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordViewModel model)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Remove existing password
                await _userManager.RemovePasswordAsync(user);
                
                // Add new password
                var result = await _userManager.AddPasswordAsync(user, model.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = $"Password reset failed: {errors}" });
                }

                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                _logger.LogInformation("Password reset for user {Email} by {AdminEmail}", 
                    user.Email, currentUser!.Email);

                return Json(new { success = true, message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", id);
                return Json(new { success = false, message = "An error occurred while resetting the password" });
            }
        }

        // Helper methods
        private List<string> GetAvailableRolesForUser(string userRole)
        {
            var userLevel = RoleHierarchy.GetValueOrDefault(userRole, 0);
            
            return RoleHierarchy
                .Where(r => r.Value < userLevel && r.Key != "SuperAdmin")
                .OrderByDescending(r => r.Value)
                .Select(r => r.Key)
                .ToList();
        }

        private bool CanAssignRole(string currentUserRole, string targetRole)
        {
            var currentLevel = RoleHierarchy.GetValueOrDefault(currentUserRole, 0);
            var targetLevel = RoleHierarchy.GetValueOrDefault(targetRole, 0);
            
            return currentLevel > targetLevel && targetRole != "SuperAdmin";
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
