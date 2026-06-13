using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Core.Events; // Fixed
using Nop.Services.Events; // Fixed
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Escrow.Services;

namespace Nop.Plugin.Marketplace.Escrow.Consumers
{
    public class OrderPaidEventConsumer : IConsumer<OrderPaidEvent>
    {
        private readonly IEscrowService _escrowService;
        public OrderPaidEventConsumer(IEscrowService escrowService) { _escrowService = escrowService; }

        public async Task HandleEventAsync(OrderPaidEvent eventMessage)
        {
            await _escrowService.TransitionStateByOrderIdAsync(eventMessage.Order.Id, EscrowState.Funded, "Payment confirmed.");
        }
    }
}