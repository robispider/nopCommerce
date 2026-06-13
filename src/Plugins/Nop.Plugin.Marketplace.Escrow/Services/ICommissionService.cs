using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Core.Domains;

namespace Nop.Plugin.Marketplace.Escrow.Services
{
    public interface ICommissionService
    {
        /// <summary>
        /// Calculates the exact financial split for a specific Escrow Transaction / Order.
        /// </summary>
        Task<CommissionSplitResult> CalculateSplitsAsync(int coreOrderId);
    }
}