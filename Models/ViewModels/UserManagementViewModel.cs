using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IT15_Project.Models.ViewModels
{
    /// <summary>
    /// ViewModel for User Management Index page
    /// Contains user list and summary metrics
    /// </summary>
    public class UserManagementViewModel
    {
        // Summary Metrics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TechnicianCount { get; set; }
        public int AdminCount { get; set; }

        // User List
        public List<UserListItemDto> Users { get; set; } = new List<UserListItemDto>();

        // Filter Options
        public List<string> AvailableRoles { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO for user list item display
    /// </summary>
    public class UserListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Personnel linkage
        public bool HasPersonnelLink { get; set; }
        public int? PersonnelId { get; set; }
        public string? PersonnelName { get; set; }
    }

    /// <summary>
    /// ViewModel for User Details page
    /// </summary>
    public class UserDetailsViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        
        // Company
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        
        // Roles
        public List<string> Roles { get; set; } = new List<string>();
        
        // Personnel Link
        public bool HasPersonnelLink { get; set; }
        public int? PersonnelId { get; set; }
        public string? PersonnelName { get; set; }
        public string? PersonnelRole { get; set; }
        public string? PersonnelSkillSet { get; set; }
        
        // Activity
        public int WorkOrdersCreated { get; set; }
        public int WorkOrdersAssigned { get; set; }
        
        // Computed
        public int AccountAgeDays => (DateTime.Now - CreatedAt).Days;
        public string AccountStatus => IsActive ? "Active" : "Inactive";
    }

    /// <summary>
    /// ViewModel for Create/Edit User
    /// </summary>
    public class CreateEditUserViewModel
    {
        public string? Id { get; set; } // Null for create, populated for edit
        
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(200, ErrorMessage = "Full name cannot exceed 200 characters")]
        public string FullName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(50)]
        public string? PhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        // Optional Personnel link
        public int? LinkedPersonnelId { get; set; }
        
        // Password (only for create)
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }
        
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }
        
        // Available options for dropdowns
        public List<string> AvailableRoles { get; set; } = new List<string>();
        public List<PersonnelOptionDto> AvailablePersonnel { get; set; } = new List<PersonnelOptionDto>();
    }

    /// <summary>
    /// DTO for personnel dropdown options
    /// </summary>
    public class PersonnelOptionDto
    {
        public int PersonnelId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Role { get; set; }
        public bool HasUserAccount { get; set; }
    }

    /// <summary>
    /// ViewModel for Change Role operation
    /// </summary>
    public class ChangeRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "New role is required")]
        public string NewRole { get; set; } = string.Empty;
        
        public List<string> AvailableRoles { get; set; } = new List<string>();
    }

    /// <summary>
    /// ViewModel for Reset Password operation
    /// </summary>
    public class ResetPasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
        
        public bool SendEmailNotification { get; set; } = true;
    }
}
