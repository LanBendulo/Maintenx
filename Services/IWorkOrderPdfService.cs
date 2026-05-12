namespace IT15_Project.Services
{
    /// <summary>
    /// Service interface for generating Work Order PDF reports
    /// </summary>
    public interface IWorkOrderPdfService
    {
        /// <summary>
        /// Generates a professional PDF report for a work order
        /// </summary>
        /// <param name="workOrderId">Work order ID</param>
        /// <param name="companyId">Company ID for multi-tenant security</param>
        /// <returns>PDF file as byte array</returns>
        Task<byte[]> GenerateWorkOrderPdfAsync(int workOrderId, int companyId);
    }
}
