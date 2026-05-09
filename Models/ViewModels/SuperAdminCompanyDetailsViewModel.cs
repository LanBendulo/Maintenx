using System;
using System.Collections.Generic;

namespace IT15_Project.Models.ViewModels
{
    /// <summary>
    /// ViewModel for SuperAdmin Company Details page
    /// Contains all data needed for company details display
    /// </summary>
    public class SuperAdminCompanyDetailsViewModel
    {
        // Company Information
        public Company Company { get; set; } = null!;

        // Metrics
        public int UserCount { get; set; }
        public int AssetCount { get; set; }
        public int WorkOrderCount { get; set; }
        public int MaintenanceRequestCount { get; set; }
        public int TechnicianCount { get; set; }

        // Subscription Information
        public int? DaysRemaining { get; set; }
        public string SubscriptionStatus { get; set; } = "Active"; // Active, Expiring, Expired

        // Recent Activity
        public List<RecentWorkOrderDto> RecentWorkOrders { get; set; } = new List<RecentWorkOrderDto>();
        public List<RecentMaintenanceRequestDto> RecentRequests { get; set; } = new List<RecentMaintenanceRequestDto>();
        public List<RecentUserDto> RecentUsers { get; set; } = new List<RecentUserDto>();
    }

    public class RecentWorkOrderDto
    {
        public int WorkOrderId { get; set; }
        public string WorkOrderNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
    }

    public class RecentMaintenanceRequestDto
    {
        public int RequestId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string IssueDescription { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class RecentUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string? Personnel { get; set; }
    }
}
