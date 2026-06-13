using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Core.Domains;

namespace Nop.Plugin.Marketplace.Commission.Services
{
    public interface ICommissionEvaluatorService
    {
        /// <summary>
        /// Reads the exact allocations, finds the highest priority rule for each item, and generates the exact split.
        /// </summary>
        Task<CommissionSplitResult> CalculateSplitsAsync(int coreOrderId);

        /// <summary>
        /// Reads the exact allocations from the immutable ledger. Throws if not found.
        /// </summary>
        Task<CommissionSplitResult> GetExistingSplitsAsync(int coreOrderId);
    }
}