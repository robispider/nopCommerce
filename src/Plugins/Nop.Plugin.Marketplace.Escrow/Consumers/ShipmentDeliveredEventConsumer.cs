using System.Threading.Tasks;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events; // Fixed
using Nop.Services.Events; // Fixed
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Escrow.Services;

namespace Nop.Plugin.Marketplace.Escrow.Consumers
{
    public class ShipmentDeliveredEventConsumer : IConsumer<ShipmentDeliveredEvent>
    {
        private readonly IEscrowService _escrowService;
        public ShipmentDeliveredEventConsumer(IEscrowService escrowService) { _escrowService = escrowService; }

        public async Task HandleEventAsync(ShipmentDeliveredEvent eventMessage)
        {
            await _escrowService.TransitionStateByOrderIdAsync(eventMessage.Shipment.OrderId, EscrowState.Delivered, "Delivered");
            await _escrowService.TransitionStateByOrderIdAsync(eventMessage.Shipment.OrderId, EscrowState.GracePeriod, "Grace Period");
        }
    }
}