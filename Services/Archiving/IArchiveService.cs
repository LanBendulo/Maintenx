using IT15_Project.Models;

namespace IT15_Project.Services.Archiving
{
    /// <summary>
    /// Service for managing soft archival of operational records
    /// Implements lifecycle-governed retention instead of destructive deletion
    /// </summary>
    public interface IArchiveService
    {
        /// <summary>
        /// Archives a work order (soft delete with audit trail)
        /// </summary>
        /// <param name="workOrderId">Work order to archive</param>
        /// <param name="userId">User performing the archive</param>
        /// <param name="reason">Reason for archiving</param>
        /// <returns>Success status and message</returns>
        Task<(bool Success, string Message)> ArchiveWorkOrderAsync(int workOrderId, string userId, string reason);

        /// <summary>
        /// Restores an archived work order
        /// </summary>
        /// <param name="workOrderId">Work order to restore</param>
        /// <param name="userId">User performing the restore</param>
        /// <returns>Success status and message</returns>
        Task<(bool Success, string Message)> RestoreWorkOrderAsync(int workOrderId, string userId);

        /// <summary>
        /// Checks if a work order can be archived based on lifecycle rules
        /// </summary>
        /// <param name="workOrderId">Work order to check</param>
        /// <returns>Can archive status and validation message</returns>
        Task<(bool CanArchive, string Message)> CanArchiveWorkOrderAsync(int workOrderId);

        /// <summary>
        /// Gets archived work orders for a company
        /// </summary>
        /// <param name="companyId">Company ID</param>
        /// <returns>List of archived work orders</returns>
        Task<List<WorkOrder>> GetArchivedWorkOrdersAsync(int companyId);
    }
}
