using IT15_Project.Models;

namespace IT15_Project.Services.Parts
{
    /// <summary>
    /// Service for managing parts usage in work orders
    /// Implements staged workflow with lifecycle governance
    /// </summary>
    public interface IPartsService
    {
        /// <summary>
        /// Stage a part for use in a work order (technician action)
        /// Validates stock availability but does NOT deduct inventory
        /// </summary>
        Task<(bool Success, string Message, WorkOrderPart? Part)> StagePartUsageAsync(
            int workOrderId,
            int partId,
            int quantityUsed,
            int personnelId,
            int companyId);

        /// <summary>
        /// Remove staged part usage (technician action)
        /// Only allowed while WO is active and usage is Pending
        /// </summary>
        Task<(bool Success, string Message)> RemoveStagedPartAsync(
            int workOrderPartId,
            int personnelId,
            int companyId);

        /// <summary>
        /// Update staged part quantity (technician action)
        /// Only allowed while WO is active and usage is Pending
        /// </summary>
        Task<(bool Success, string Message)> UpdateStagedPartQuantityAsync(
            int workOrderPartId,
            int newQuantity,
            int personnelId,
            int companyId);

        /// <summary>
        /// Get all staged parts for a work order
        /// </summary>
        Task<List<WorkOrderPart>> GetStagedPartsAsync(int workOrderId, int companyId);

        /// <summary>
        /// Validate if part can be staged (stock availability check)
        /// Does NOT modify inventory
        /// </summary>
        Task<(bool CanStage, string Message, int AvailableStock)> ValidatePartAvailabilityAsync(
            int partId,
            int requestedQuantity,
            int companyId,
            int? excludeWorkOrderPartId = null);

        /// <summary>
        /// Consume staged parts (finalize inventory deduction)
        /// Called during WO completion or supervisor approval
        /// Creates inventory movement logs
        /// </summary>
        Task<(bool Success, string Message)> ConsumeWorkOrderPartsAsync(
            int workOrderId,
            string userId,
            int companyId);

        /// <summary>
        /// Get available parts for selection (active, in-stock)
        /// Hides cost information for technicians
        /// </summary>
        Task<List<Part>> GetAvailablePartsAsync(int companyId, bool includeCost = false);
    }
}
