using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Escrow.Services;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Escrow.Consumers
{
    public class OrderRefundedEventConsumer : IConsumer<OrderRefundedEvent>
    {
        private readonly IEscrowService _escrowService;

        public OrderRefundedEventConsumer(IEscrowService escrowService)
        {
            _escrowService = escrowService;
        }

        public async Task HandleEventAsync(OrderRefundedEvent eventMessage)
        {
            // The customer got their money back. 
            // Instantly transition escrow to Refunded to prevent the Outbox from paying the vendor.
            await _escrowService.TransitionStateByOrderIdAsync(
                eventMessage.Order.Id,
                EscrowState.Refunded,
                "Native Order Refunded. Escrow cancelled."
            );
        }
    }
}