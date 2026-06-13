using System.Threading.Tasks;
using Nop.Core.Events;
using Nop.Services.Events;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Wallet.Services;

namespace Nop.Plugin.Marketplace.Wallet.Consumers
{
    public class SettlementRequestedEventConsumer : IConsumer<SettlementRequestedEvent>
    {
        private readonly IWalletTransactionService _walletTransactionService;

        public SettlementRequestedEventConsumer(IWalletTransactionService walletTransactionService)
        {
            _walletTransactionService = walletTransactionService;
        }

        public async Task HandleEventAsync(SettlementRequestedEvent eventMessage)
        {
            // We now call the single unified method!
            await _walletTransactionService.ProcessSettlementAsync(eventMessage);
        }
    }
}