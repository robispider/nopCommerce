using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Wallet.Services;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Wallet.Consumers
{
    public class EscrowRefundedEventConsumer : IConsumer<EscrowRefundedEvent>
    {
        private readonly IWalletTransactionService _walletTransactionService;

        public EscrowRefundedEventConsumer(IWalletTransactionService walletTransactionService)
        {
            _walletTransactionService = walletTransactionService;
        }

        public async Task HandleEventAsync(EscrowRefundedEvent eventMessage)
        {
            // Returns funds to the Reseller
            await _walletTransactionService.ProcessRefundAsync(eventMessage);
        }
    }
}