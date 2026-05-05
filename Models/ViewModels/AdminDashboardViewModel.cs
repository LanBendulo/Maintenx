using System;
using System.Collections.Generic;

namespace IT15_Project.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Admin Dashboard
    /// Contains all data needed for dashboard display
    /// </summary>
    public class AdminDashboardViewModel
    {
        // Summary Cards
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ActiveWorkOrders { get; set; }
        public int TotalAssets { get; set; }

        // Request Status Chart Data
        public Dictionary<string, int> RequestStatusCounts { get; set; } = new Dictionary<string, int>();

        // Ongoing Work Orders
        public List<OngoingWorkOrderDto> OngoingWorkOrders { get; set; } = new List<OngoingWorkOrderDto>();

        // Active Alerts
        public List<DashboardAlertDto> Alerts { get; set; } = new List<DashboardAlertDto>();
    }

    public class OngoingWorkOrderDto
    {
        public int WorkOrderId { get; set; }
        public string WorkOrderNumber { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string TechnicianName { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public int ProgressPercentage { get; set; } = 0;
    }

    public class DashboardAlertDto
    {
        public string Type { get; set; } = string.Empty; // "overdue", "upcoming-pm", "low-stock"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public string AssetOrLocation { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // "high", "medium", "low"
    }
}
