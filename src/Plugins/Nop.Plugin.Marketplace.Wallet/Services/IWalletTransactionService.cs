using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Core.Events;

namespace Nop.Plugin.Marketplace.Wallet.Services
{
    public interface IWalletTransactionService
    {
        Task ProcessEscrowReleaseAsync(SettlementRequestedEvent releaseEvent);

        
        Task<int> RequestWithdrawalAsync(int vendorId, decimal amount);
        Task ApproveWithdrawalAsync(int withdrawalRequestId, string adminNotes = null);
        Task RejectWithdrawalAsync(int withdrawalRequestId, string adminNotes = null);
        Task ProcessReserveHoldAsync(ReserveHoldRequestedEvent holdEvent);
        Task ProcessReserveReleaseAsync(ReserveReleasedEvent releaseEvent);

        
    }
}