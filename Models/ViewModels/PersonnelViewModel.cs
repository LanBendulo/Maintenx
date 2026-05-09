using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IT15_Project.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Personnel Index page
    /// Contains personnel list and summary metrics
    /// </summary>
    public class PersonnelIndexViewModel
    {
        // Summary Metrics
        public int TotalPersonnel { get; set; }
        public int ActivePersonnel { get; set; }
        public int TechnicianCount { get; set; }
        public int SupervisorCount { get; set; }
        public int ContractorCount { get; set; }

        // Personnel List
        public List<PersonnelListItemDto> Personnel { get; set; } = new List<PersonnelListItemDto>();

        // Filter Options
        public List<string> Departments { get; set; } = new List<string>();
        public List<string> EmploymentTypes { get; set; } = new List<string>();
        public List<string> Statuses { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO for personnel list item display
    /// </summary>
    public class PersonnelListItemDto
    {
        public int PersonnelId { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public string Status { get; set; } = "Active";
        public bool HasUserAccount { get; set; }
        public string? UserEmail { get; set; }
        public int ActiveWorkOrders { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// ViewModel for Personnel Details page
    /// </summary>
    public class PersonnelDetailsViewModel
    {
        // Basic Information
        public int PersonnelId { get; set; }
        public string? EmployeeId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        // Employment Information
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string Status { get; set; } = "Active";
        public string? Role { get; set; }
        public string? SkillSet { get; set; }
        public decimal? HourlyRate { get; set; }

        // Emergency Contact
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }

        // Additional
        public string? Notes { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // User Account Link
        public bool HasUserAccount { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserFullName { get; set; }
        public List<string> UserRoles { get; set; } = new List<string>();

        // Company
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }

        // Work Order Metrics
        public int ActiveWorkOrders { get; set; }
        public int CompletedWorkOrders { get; set; }
        public int TotalWorkOrders { get; set; }

        // Recent Work Orders
        public List<RecentWorkOrderDto> RecentWorkOrders { get; set; } = new List<RecentWorkOrderDto>();

        // Computed
        public int DaysEmployed => HireDate.HasValue ? (DateTime.Now - HireDate.Value).Days : 0;
        public bool IsTerminated => Status == "Terminated" || TerminationDate.HasValue;
    }

    /// <summary>
    /// ViewModel for Create/Edit Personnel
    /// </summary>
    public class PersonnelCreateEditViewModel
    {
        public int? PersonnelId { get; set; } // Null for create, populated for edit

        [Required(ErrorMessage = "Employee ID is required")]
        [StringLength(50, ErrorMessage = "Employee ID cannot exceed 50 characters")]
        [Display(Name = "Employee ID")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters")]
        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(256)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(50)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(100)]
        [Display(Name = "Position/Title")]
        public string? Position { get; set; }

        [StringLength(100)]
        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Employment Type")]
        public string? EmploymentType { get; set; }

        [Display(Name = "Hire Date")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        [Display(Name = "Termination Date")]
        [DataType(DataType.Date)]
        public DateTime? TerminationDate { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";

        [StringLength(50)]
        [Display(Name = "Job Role")]
        public string? Role { get; set; }

        [StringLength(255)]
        [Display(Name = "Skills")]
        public string? SkillSet { get; set; }

        [Display(Name = "Hourly Rate")]
        [Range(0, 9999.99, ErrorMessage = "Hourly rate must be between 0 and 9999.99")]
        public decimal? HourlyRate { get; set; }

        [StringLength(200)]
        [Display(Name = "Emergency Contact Name")]
        public string? EmergencyContactName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(50)]
        [Display(Name = "Emergency Contact Phone")]
        public string? EmergencyContactPhone { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Optional User Account Link
        [Display(Name = "Link to User Account")]
        public string? UserId { get; set; }

        // Available options for dropdowns
        public List<string> AvailableEmploymentTypes { get; set; } = new List<string>();
        public List<string> AvailableStatuses { get; set; } = new List<string>();
        public List<string> AvailableDepartments { get; set; } = new List<string>();
        public List<UserAccountOptionDto> AvailableUserAccounts { get; set; } = new List<UserAccountOptionDto>();
    }

    /// <summary>
    /// DTO for user account dropdown options
    /// </summary>
    public class UserAccountOptionDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsLinked { get; set; }
    }

    /// <summary>
    /// ViewModel for Link User Account operation
    /// </summary>
    public class LinkUserAccountViewModel
    {
        public int PersonnelId { get; set; }
        public string PersonnelName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a user account")]
        public string UserId { get; set; } = string.Empty;

        public List<UserAccountOptionDto> AvailableUsers { get; set; } = new List<UserAccountOptionDto>();
    }

    /// <summary>
    /// ViewModel for Create User Account from Personnel
    /// </summary>
    public class CreateUserFromPersonnelViewModel
    {
        public int PersonnelId { get; set; }
        public string PersonnelName { get; set; } = string.Empty;
        public string PersonnelEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        public List<string> AvailableRoles { get; set; } = new List<string>();
    }
}
