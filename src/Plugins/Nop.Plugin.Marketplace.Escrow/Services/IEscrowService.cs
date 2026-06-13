using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Core.Domains;

namespace Nop.Plugin.Marketplace.Escrow.Services
{
    public interface IEscrowService
    {
        Task TransitionStateByOrderIdAsync(int coreOrderId, EscrowState newState, string systemNote);
        Task ReleaseFundsAsync(int escrowTransactionId, int adminUserId = 0);
        Task DisputeEscrowAsync(int escrowTransactionId, string reason, int adminUserId);
        Task MarkAsSettledAsync(int escrowTransactionId);
    }
}