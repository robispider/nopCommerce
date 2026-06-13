using System.Threading.Tasks;
using Nop.Core.Events; // Fixed
using Nop.Services.Events; // Fixed
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Wallet.Services;

namespace Nop.Plugin.Marketplace.Wallet.Consumers
{
    public class EscrowReleasedEventConsumer : IConsumer<SettlementRequestedEvent>
    {
        private readonly IWalletTransactionService _walletTransactionService;
        public EscrowReleasedEventConsumer(IWalletTransactionService walletTransactionService)
        {
            _walletTransactionService = walletTransactionService;
        }

        public async Task HandleEventAsync(SettlementRequestedEvent eventMessage)
        {
            await _walletTransactionService.ProcessEscrowReleaseAsync(eventMessage);
        }
    }
}