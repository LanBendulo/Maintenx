using IT15_Project.Constants;
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
    /// Personnel Management Controller
    /// Manages workforce records (employees, technicians, contractors)
    /// Separate from User Management - Personnel = business entity, User = authentication
    /// Multi-tenant with proper isolation
    /// </summary>
    [Authorize(Roles = "Owner,Admin,Supervisor")]
    [Route("admin/personnel")]
    public class PersonnelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITenantService _tenantService;
        private readonly ILogger<PersonnelController> _logger;

        public PersonnelController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ITenantService tenantService,
            ILogger<PersonnelController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantService = tenantService;
            _logger = logger;
        }

        /// <summary>
        /// Index - Personnel list with summary metrics
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                // Get all personnel for this company
                var personnel = await _context.Personnel
                    .AsNoTracking()
                    .Include(p => p.User)
                    .Where(p => p.CompanyId == companyId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                // Build personnel list with work order counts
                var personnelList = new List<PersonnelListItemDto>();

                foreach (var person in personnel)
                {
                    var activeWorkOrders = await _context.WorkOrders
                        .CountAsync(w => w.CompanyId == companyId &&
                                        w.AssignedTo == person.PersonnelId &&
                                        w.Status != WorkOrderStatuses.Completed && 
                                        w.Status != WorkOrderStatuses.Cancelled);

                    personnelList.Add(new PersonnelListItemDto
                    {
                        PersonnelId = person.PersonnelId,
                        EmployeeId = person.EmployeeId ?? "N/A",
                        FullName = person.FullName,
                        Position = person.Position,
                        Department = person.Department,
                        EmploymentType = person.EmploymentType,
                        Status = person.Status,
                        HasUserAccount = person.HasUserAccount,
                        UserEmail = person.User?.Email,
                        ActiveWorkOrders = activeWorkOrders,
                        HireDate = person.HireDate,
                        CreatedAt = person.CreatedAt
                    });
                }

                // Calculate metrics
                var viewModel = new PersonnelIndexViewModel
                {
                    TotalPersonnel = personnelList.Count,
                    ActivePersonnel = personnelList.Count(p => p.Status == "Active"),
                    TechnicianCount = personnelList.Count(p => p.Position?.Contains("Technician", StringComparison.OrdinalIgnoreCase) == true),
                    SupervisorCount = personnelList.Count(p => p.Position?.Contains("Supervisor", StringComparison.OrdinalIgnoreCase) == true),
                    ContractorCount = personnelList.Count(p => p.EmploymentType == "Contractor"),
                    Personnel = personnelList,
                    Departments = personnel.Where(p => !string.IsNullOrEmpty(p.Department))
                                          .Select(p => p.Department!)
                                          .Distinct()
                                          .OrderBy(d => d)
                                          .ToList(),
                    EmploymentTypes = EmploymentType.GetAll(),
                    Statuses = PersonnelStatus.GetAll()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading personnel index");
                throw; // Re-throw to show full exception in Development mode
            }
        }

        /// <summary>
        /// Details - View personnel profile and work history
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                var personnel = await _context.Personnel
                    .AsNoTracking()
                    .Include(p => p.Company)
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PersonnelId == id && p.CompanyId == companyId);

                if (personnel == null)
                {
                    return NotFound();
                }

                // Get user roles if linked
                var userRoles = new List<string>();
                if (personnel.User != null)
                {
                    userRoles = (await _userManager.GetRolesAsync(personnel.User)).ToList();
                }

                // Get work order metrics
                var activeWorkOrders = await _context.WorkOrders
                    .CountAsync(w => w.CompanyId == companyId &&
                                    w.AssignedTo == personnel.PersonnelId &&
                                    w.Status != WorkOrderStatuses.Completed && 
                                    w.Status != WorkOrderStatuses.Cancelled);

                var completedWorkOrders = await _context.WorkOrders
                    .CountAsync(w => w.CompanyId == companyId &&
                                    w.AssignedTo == personnel.PersonnelId &&
                                    w.Status == WorkOrderStatuses.Completed);

                var totalWorkOrders = await _context.WorkOrders
                    .CountAsync(w => w.CompanyId == companyId &&
                                    w.AssignedTo == personnel.PersonnelId);

                // Get recent work orders
                var recentWorkOrders = await _context.WorkOrders
                    .AsNoTracking()
                    .Include(w => w.Asset)
                    .Where(w => w.CompanyId == companyId && w.AssignedTo == personnel.PersonnelId)
                    .OrderByDescending(w => w.DateCreated)
                    .ThenByDescending(w => w.WorkOrderId)
                    .Take(10)
                    .Select(w => new RecentWorkOrderDto
                    {
                        WorkOrderId = w.WorkOrderId,
                        WorkOrderNumber = $"WO-{w.WorkOrderId.ToString().PadLeft(4, '0')}",
                        AssetName = w.Asset != null ? w.Asset.AssetName : "N/A",
                        Status = w.Status ?? "Unknown",
                        Priority = w.Priority ?? "Medium",
                        DateCreated = w.DateCreated ?? DateTime.Now
                    })
                    .ToListAsync();

                var viewModel = new PersonnelDetailsViewModel
                {
                    PersonnelId = personnel.PersonnelId,
                    EmployeeId = personnel.EmployeeId,
                    FirstName = personnel.FirstName,
                    MiddleName = personnel.MiddleName,
                    LastName = personnel.LastName,
                    FullName = personnel.FullName,
                    Email = personnel.Email,
                    PhoneNumber = personnel.PhoneNumber,
                    Address = personnel.Address,
                    Position = personnel.Position,
                    Department = personnel.Department,
                    EmploymentType = personnel.EmploymentType,
                    HireDate = personnel.HireDate,
                    TerminationDate = personnel.TerminationDate,
                    Status = personnel.Status,
                    Role = personnel.Role,
                    SkillSet = personnel.SkillSet,
                    HourlyRate = personnel.HourlyRate,
                    EmergencyContactName = personnel.EmergencyContactName,
                    EmergencyContactPhone = personnel.EmergencyContactPhone,
                    Notes = personnel.Notes,
                    ProfilePhotoUrl = personnel.ProfilePhotoUrl,
                    IsActive = personnel.IsActive,
                    CreatedAt = personnel.CreatedAt,
                    UpdatedAt = personnel.UpdatedAt,
                    HasUserAccount = personnel.HasUserAccount,
                    UserId = personnel.UserId,
                    UserEmail = personnel.User?.Email,
                    UserFullName = personnel.User?.FullName,
                    UserRoles = userRoles,
                    CompanyId = personnel.CompanyId,
                    CompanyName = personnel.Company?.CompanyName,
                    ActiveWorkOrders = activeWorkOrders,
                    CompletedWorkOrders = completedWorkOrders,
                    TotalWorkOrders = totalWorkOrders,
                    RecentWorkOrders = recentWorkOrders
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading personnel details for ID: {PersonnelId}", id);
                return View("Error");
            }
        }

        /// <summary>
        /// Create - Show create personnel form
        /// </summary>
        [HttpGet("create")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Create()
        {
            var companyId = _tenantService.GetCurrentCompanyId();

            // Get available user accounts without personnel links
            var availableUsers = await _context.Users
                .AsNoTracking()
                .Where(u => u.CompanyId == companyId && 
                           !_context.Personnel.Any(p => p.UserId == u.Id))
                .Select(u => new UserAccountOptionDto
                {
                    UserId = u.Id,
                    FullName = u.FullName ?? "N/A",
                    Email = u.Email ?? "N/A",
                    IsLinked = false
                })
                .ToListAsync();

            // Get existing departments for dropdown
            var departments = await _context.Personnel
                .Where(p => p.CompanyId == companyId && !string.IsNullOrEmpty(p.Department))
                .Select(p => p.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            var viewModel = new PersonnelCreateEditViewModel
            {
                IsActive = true,
                Status = "Active",
                AvailableEmploymentTypes = EmploymentType.GetAll(),
                AvailableStatuses = PersonnelStatus.GetAll(),
                AvailableDepartments = departments,
                AvailableUserAccounts = availableUsers
            };

            return View(viewModel);
        }

        /// <summary>
        /// Create - Process create personnel form
        /// </summary>
        [HttpPost("create")]
        [Authorize(Roles = "Owner,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PersonnelCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();

                // Check if employee ID already exists
                if (!string.IsNullOrEmpty(model.EmployeeId))
                {
                    var existingEmployee = await _context.Personnel
                        .AnyAsync(p => p.CompanyId == companyId && p.EmployeeId == model.EmployeeId);

                    if (existingEmployee)
                    {
                        ModelState.AddModelError("EmployeeId", "An employee with this ID already exists");
                        return View(model);
                    }
                }

                // Validate user link if specified
                if (!string.IsNullOrEmpty(model.UserId))
                {
                    var user = await _context.Users.FindAsync(model.UserId);
                    if (user == null || user.CompanyId != companyId)
                    {
                        ModelState.AddModelError("UserId", "Invalid user account selected");
                        return View(model);
                    }

                    // Check if user is already linked
                    var existingLink = await _context.Personnel
                        .AnyAsync(p => p.UserId == model.UserId);

                    if (existingLink)
                    {
                        ModelState.AddModelError("UserId", "This user account is already linked to another personnel record");
                        return View(model);
                    }
                }

                // Create personnel
                var personnel = new Personnel
                {
                    CompanyId = companyId,
                    UserId = string.IsNullOrEmpty(model.UserId) ? null : model.UserId,
                    EmployeeId = model.EmployeeId,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    Position = model.Position,
                    Department = model.Department,
                    EmploymentType = model.EmploymentType,
                    HireDate = model.HireDate,
                    TerminationDate = model.TerminationDate,
                    Status = model.Status,
                    Role = model.Role,
                    SkillSet = model.SkillSet,
                    HourlyRate = model.HourlyRate,
                    EmergencyContactName = model.EmergencyContactName,
                    EmergencyContactPhone = model.EmergencyContactPhone,
                    Notes = model.Notes,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                _context.Personnel.Add(personnel);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                _logger.LogInformation("Personnel {FullName} created by {AdminEmail}", 
                    personnel.FullName, currentUser!.Email);

                TempData["SuccessMessage"] = $"Personnel {personnel.FullName} created successfully";
                return RedirectToAction(nameof(Details), new { id = personnel.PersonnelId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating personnel");
                ModelState.AddModelError("", "An error occurred while creating the personnel record");
                return View(model);
            }
        }

        /// <summary>
        /// Edit - Show edit personnel form
        /// </summary>
        [HttpGet("{id}/edit")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var personnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.PersonnelId == id && p.CompanyId == companyId);

                if (personnel == null)
                {
                    return NotFound();
                }

                // Get available user accounts
                var availableUsers = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.CompanyId == companyId && 
                               (u.Id == personnel.UserId || !_context.Personnel.Any(p => p.UserId == u.Id)))
                    .Select(u => new UserAccountOptionDto
                    {
                        UserId = u.Id,
                        FullName = u.FullName ?? "N/A",
                        Email = u.Email ?? "N/A",
                        IsLinked = u.Id == personnel.UserId
                    })
                    .ToListAsync();

                // Get existing departments
                var departments = await _context.Personnel
                    .Where(p => p.CompanyId == companyId && !string.IsNullOrEmpty(p.Department))
                    .Select(p => p.Department!)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                var viewModel = new PersonnelCreateEditViewModel
                {
                    PersonnelId = personnel.PersonnelId,
                    EmployeeId = personnel.EmployeeId ?? "",
                    FirstName = personnel.FirstName,
                    MiddleName = personnel.MiddleName,
                    LastName = personnel.LastName,
                    Email = personnel.Email,
                    PhoneNumber = personnel.PhoneNumber,
                    Address = personnel.Address,
                    Position = personnel.Position,
                    Department = personnel.Department,
                    EmploymentType = personnel.EmploymentType,
                    HireDate = personnel.HireDate,
                    TerminationDate = personnel.TerminationDate,
                    Status = personnel.Status,
                    Role = personnel.Role,
                    SkillSet = personnel.SkillSet,
                    HourlyRate = personnel.HourlyRate,
                    EmergencyContactName = personnel.EmergencyContactName,
                    EmergencyContactPhone = personnel.EmergencyContactPhone,
                    Notes = personnel.Notes,
                    IsActive = personnel.IsActive,
                    UserId = personnel.UserId,
                    AvailableEmploymentTypes = EmploymentType.GetAll(),
                    AvailableStatuses = PersonnelStatus.GetAll(),
                    AvailableDepartments = departments,
                    AvailableUserAccounts = availableUsers
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for personnel {PersonnelId}", id);
                return View("Error");
            }
        }

        /// <summary>
        /// Edit - Process edit personnel form
        /// </summary>
        [HttpPost("{id}/edit")]
        [Authorize(Roles = "Owner,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PersonnelCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var personnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.PersonnelId == id && p.CompanyId == companyId);

                if (personnel == null)
                {
                    return NotFound();
                }

                // Check if employee ID already exists (excluding current record)
                if (!string.IsNullOrEmpty(model.EmployeeId))
                {
                    var existingEmployee = await _context.Personnel
                        .AnyAsync(p => p.CompanyId == companyId && 
                                      p.EmployeeId == model.EmployeeId && 
                                      p.PersonnelId != id);

                    if (existingEmployee)
                    {
                        ModelState.AddModelError("EmployeeId", "An employee with this ID already exists");
                        return View(model);
                    }
                }

                // Validate user link if specified
                if (!string.IsNullOrEmpty(model.UserId))
                {
                    var user = await _context.Users.FindAsync(model.UserId);
                    if (user == null || user.CompanyId != companyId)
                    {
                        ModelState.AddModelError("UserId", "Invalid user account selected");
                        return View(model);
                    }

                    // Check if user is already linked (excluding current record)
                    var existingLink = await _context.Personnel
                        .AnyAsync(p => p.UserId == model.UserId && p.PersonnelId != id);

                    if (existingLink)
                    {
                        ModelState.AddModelError("UserId", "This user account is already linked to another personnel record");
                        return View(model);
                    }
                }

                // Update personnel
                personnel.UserId = string.IsNullOrEmpty(model.UserId) ? null : model.UserId;
                personnel.EmployeeId = model.EmployeeId;
                personnel.FirstName = model.FirstName;
                personnel.MiddleName = model.MiddleName;
                personnel.LastName = model.LastName;
                personnel.Email = model.Email;
                personnel.PhoneNumber = model.PhoneNumber;
                personnel.Address = model.Address;
                personnel.Position = model.Position;
                personnel.Department = model.Department;
                personnel.EmploymentType = model.EmploymentType;
                personnel.HireDate = model.HireDate;
                personnel.TerminationDate = model.TerminationDate;
                personnel.Status = model.Status;
                personnel.Role = model.Role;
                personnel.SkillSet = model.SkillSet;
                personnel.HourlyRate = model.HourlyRate;
                personnel.EmergencyContactName = model.EmergencyContactName;
                personnel.EmergencyContactPhone = model.EmergencyContactPhone;
                personnel.Notes = model.Notes;
                personnel.IsActive = model.IsActive;
                personnel.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                _logger.LogInformation("Personnel {FullName} updated by {AdminEmail}", 
                    personnel.FullName, currentUser!.Email);

                TempData["SuccessMessage"] = $"Personnel {personnel.FullName} updated successfully";
                return RedirectToAction(nameof(Details), new { id = personnel.PersonnelId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating personnel {PersonnelId}", id);
                ModelState.AddModelError("", "An error occurred while updating the personnel record");
                return View(model);
            }
        }

        /// <summary>
        /// Archive - Soft delete personnel record
        /// </summary>
        [HttpPost("{id}/archive")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Archive(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var personnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.PersonnelId == id && p.CompanyId == companyId);

                if (personnel == null)
                {
                    return Json(new { success = false, message = "Personnel not found" });
                }

                personnel.IsActive = false;
                personnel.Status = "Inactive";
                personnel.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                _logger.LogInformation("Personnel {FullName} archived by {AdminEmail}", 
                    personnel.FullName, currentUser!.Email);

                return Json(new { success = true, message = "Personnel archived successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving personnel {PersonnelId}", id);
                return Json(new { success = false, message = "An error occurred while archiving the personnel record" });
            }
        }

        /// <summary>
        /// Reactivate - Restore archived personnel record
        /// </summary>
        [HttpPost("{id}/reactivate")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Reactivate(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var personnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.PersonnelId == id && p.CompanyId == companyId);

                if (personnel == null)
                {
                    return Json(new { success = false, message = "Personnel not found" });
                }

                personnel.IsActive = true;
                personnel.Status = "Active";
                personnel.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                _logger.LogInformation("Personnel {FullName} reactivated by {AdminEmail}", 
                    personnel.FullName, currentUser!.Email);

                return Json(new { success = true, message = "Personnel reactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating personnel {PersonnelId}", id);
                return Json(new { success = false, message = "An error occurred while reactivating the personnel record" });
            }
        }

        /// <summary>
        /// UnlinkUser - Remove user account link from personnel
        /// </summary>
        [HttpPost("{id}/unlink-user")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> UnlinkUser(int id)
        {
            try
            {
                var companyId = _tenantService.GetCurrentCompanyId();
                var personnel = await _context.Personnel
                    .FirstOrDefaultAsync(p => p.PersonnelId == id && p.CompanyId == companyId);

                if (personnel == null)
                {
                    return Json(new { success = false, message = "Personnel not found" });
                }

                if (string.IsNullOrEmpty(personnel.UserId))
                {
                    return Json(new { success = false, message = "Personnel is not linked to a user account" });
                }

                personnel.UserId = null;
                personnel.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                _logger.LogInformation("Personnel {FullName} unlinked from user account by {AdminEmail}", 
                    personnel.FullName, currentUser!.Email);

                return Json(new { success = true, message = "User account unlinked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking user from personnel {PersonnelId}", id);
                return Json(new { success = false, message = "An error occurred while unlinking the user account" });
            }
        }
    }
}
