using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;
using Nop.Plugin.Marketplace.Order.Services;

namespace Nop.Plugin.Marketplace.Order.Consumers
{
    /// <summary>
    /// This is the NEW entry point for marketplace orders. 
    /// It intercepts nopCommerce, reserves stock, and triggers the split.
    /// </summary>
    public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly IOrderAllocationService _orderAllocationService;

        public OrderPlacedEventConsumer(IOrderAllocationService orderAllocationService)
        {
            _orderAllocationService = orderAllocationService;
        }

        public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
        {
            // Atomically reserve inventory and split the order into vendor allocations
            await _orderAllocationService.SplitNativeOrderAsync(eventMessage.Order);
        }
    }
}