using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Escrow.Services;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Escrow.Events
{
    public class CustomerConfirmedReceiptEventConsumer : IConsumer<CustomerConfirmedReceiptEvent>
    {
        private readonly IEscrowService _escrowService;

        public CustomerConfirmedReceiptEventConsumer(IEscrowService escrowService)
        {
            _escrowService = escrowService;
        }

        public async Task HandleEventAsync(CustomerConfirmedReceiptEvent eventMessage)
        {
            // Escrow handles its own business logic safely
            await _escrowService.TransitionStateByOrderIdAsync(
                eventMessage.NativeOrderId,
                EscrowState.GracePeriod,
                $"Customer explicitly verified receipt. Commencing 72-hour dispute window."
            );
        }
    }
}