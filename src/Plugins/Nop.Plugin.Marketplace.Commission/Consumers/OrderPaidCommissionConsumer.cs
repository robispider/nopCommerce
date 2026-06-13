using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Plugin.Marketplace.Commission.Services;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Commission.Consumers
{
    public class OrderPaidCommissionConsumer : IConsumer<OrderPaidEvent>
    {
        private readonly ICommissionEvaluatorService _commissionService;

        public OrderPaidCommissionConsumer(ICommissionEvaluatorService commissionService)
        {
            _commissionService = commissionService;
        }

        public async Task HandleEventAsync(OrderPaidEvent eventMessage)
        {
            // Automate the split calculation on payment cleared
            await _commissionService.CalculateSplitsAsync(eventMessage.Order.Id);
        }
    }
}