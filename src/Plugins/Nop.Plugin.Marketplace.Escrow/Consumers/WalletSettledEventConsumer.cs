using System.Threading.Tasks;
using Nop.Core.Events;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Escrow.Services;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Escrow.Consumers
{
    public class WalletSettledEventConsumer : IConsumer<WalletSettledEvent>
    {
        private readonly IEscrowService _escrowService;

        public WalletSettledEventConsumer(IEscrowService escrowService)
        {
            _escrowService = escrowService;
        }

        public async Task HandleEventAsync(WalletSettledEvent eventMessage)
        {
            // Wallet confirmed it successfully credited the accounts.
            // Move from SettlementPending to Settled!
            await _escrowService.MarkAsSettledAsync(eventMessage.EscrowTransactionId);
        }
    }
}