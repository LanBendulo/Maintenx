namespace IT15_Project.Services
{
    /// <summary>
    /// Interface for Work Order cost computation and management
    /// </summary>
    public interface ICostService
    {
        /// <summary>
        /// Compute parts cost from WorkOrderPart entries
        /// </summary>
        Task<decimal> ComputePartsCostAsync(int workOrderId, int companyId);

        /// <summary>
        /// Compute total cost (labor + parts + other)
        /// </summary>
        decimal ComputeTotalCost(decimal laborCost, decimal partsCost, decimal otherCost);

        /// <summary>
        /// Get or create WorkOrderCost record
        /// </summary>
        Task<Models.WorkOrderCost> GetOrCreateWorkOrderCostAsync(int workOrderId, int companyId);

        /// <summary>
        /// Update labor and other costs (only when Work Order is Open/InProgress)
        /// </summary>
        Task<bool> UpdateCostsAsync(int workOrderId, int companyId, decimal laborCost, decimal otherCost);

        /// <summary>
        /// Recalculate and lock costs when Work Order is completed
        /// </summary>
        Task<Models.WorkOrderCost> LockCostsAsync(int workOrderId, int companyId);
    }
}
