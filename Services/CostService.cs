using IT15_Project.Data;
using IT15_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_Project.Services
{
    /// <summary>
    /// Service for Work Order cost computation and management
    /// Handles labor, parts, and other costs with tenant isolation
    /// </summary>
    public class CostService : ICostService
    {
        private readonly ApplicationDbContext _context;

        public CostService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Compute parts cost from WorkOrderPart entries
        /// Uses UnitCostSnapshot (not current Part.UnitCost)
        /// </summary>
        public async Task<decimal> ComputePartsCostAsync(int workOrderId, int companyId)
        {
            var partsCost = await _context.WorkOrderParts
                .Where(wop => wop.WorkOrderId == workOrderId && wop.CompanyId == companyId)
                .SumAsync(wop => wop.QuantityUsed * (wop.UnitCost ?? 0));

            return partsCost;
        }

        /// <summary>
        /// Compute total cost
        /// </summary>
        public decimal ComputeTotalCost(decimal laborCost, decimal partsCost, decimal otherCost)
        {
            return laborCost + partsCost + otherCost;
        }

        /// <summary>
        /// Get or create WorkOrderCost record
        /// </summary>
        public async Task<WorkOrderCost> GetOrCreateWorkOrderCostAsync(int workOrderId, int companyId)
        {
            var cost = await _context.WorkOrderCosts
                .FirstOrDefaultAsync(c => c.WorkOrderId == workOrderId && c.CompanyId == companyId);

            if (cost == null)
            {
                // Create new cost record
                var partsCost = await ComputePartsCostAsync(workOrderId, companyId);

                cost = new WorkOrderCost
                {
                    CompanyId = companyId,
                    WorkOrderId = workOrderId,
                    LaborCost = 0,
                    PartsCost = partsCost,
                    OtherCost = 0,
                    TotalCost = partsCost,
                    CreatedAt = DateTime.Now
                };

                _context.WorkOrderCosts.Add(cost);
                await _context.SaveChangesAsync();
            }

            return cost;
        }

        /// <summary>
        /// Update labor and other costs
        /// Only allowed when Work Order is Open or InProgress
        /// </summary>
        public async Task<bool> UpdateCostsAsync(int workOrderId, int companyId, decimal laborCost, decimal otherCost)
        {
            // Validate costs
            if (laborCost < 0 || otherCost < 0)
            {
                return false;
            }

            // Get Work Order to check status
            var workOrder = await _context.WorkOrders
                .FirstOrDefaultAsync(wo => wo.WorkOrderId == workOrderId && wo.CompanyId == companyId);

            if (workOrder == null)
            {
                return false;
            }

            // Only allow updates when Open or InProgress
            if (workOrder.Status != "Open" && workOrder.Status != "In Progress")
            {
                return false;
            }

            // Get or create cost record
            var cost = await GetOrCreateWorkOrderCostAsync(workOrderId, companyId);

            // Recalculate parts cost
            var partsCost = await ComputePartsCostAsync(workOrderId, companyId);

            // Update costs
            cost.LaborCost = laborCost;
            cost.PartsCost = partsCost;
            cost.OtherCost = otherCost;
            cost.TotalCost = ComputeTotalCost(laborCost, partsCost, otherCost);
            cost.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Recalculate and lock costs when Work Order is completed
        /// This is called automatically when status changes to Completed
        /// </summary>
        public async Task<WorkOrderCost> LockCostsAsync(int workOrderId, int companyId)
        {
            var cost = await GetOrCreateWorkOrderCostAsync(workOrderId, companyId);

            // Recalculate final parts cost
            var partsCost = await ComputePartsCostAsync(workOrderId, companyId);

            // Update final costs
            cost.PartsCost = partsCost;
            cost.TotalCost = ComputeTotalCost(cost.LaborCost ?? 0, partsCost, cost.OtherCost ?? 0);
            cost.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return cost;
        }
    }
}
