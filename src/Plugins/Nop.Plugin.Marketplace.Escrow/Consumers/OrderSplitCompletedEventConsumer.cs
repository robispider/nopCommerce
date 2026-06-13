using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Plugin.Marketplace.Commission.Domains;
using Nop.Plugin.Marketplace.Commission.Services;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Escrow.Domains;
using Nop.Plugin.Marketplace.Order.Events;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Escrow.Consumers
{
    public class OrderSplitCompletedEventConsumer : IConsumer<OrderSplitCompletedEvent>
    {
        private readonly IRepository<EscrowTransaction> _escrowRepository;
        private readonly IRepository<EscrowStateHistory> _historyRepository;
        private readonly IRepository<CommissionSplit> _splitRepository;
        private readonly ICommissionEvaluatorService _commissionEvaluator;

        public OrderSplitCompletedEventConsumer(
            IRepository<EscrowTransaction> escrowRepository,
            IRepository<EscrowStateHistory> historyRepository,
            IRepository<CommissionSplit> splitRepository,
            ICommissionEvaluatorService commissionEvaluator)
        {
            _escrowRepository = escrowRepository;
            _historyRepository = historyRepository;
            _splitRepository = splitRepository;
            _commissionEvaluator = commissionEvaluator;
        }

        public async Task HandleEventAsync(OrderSplitCompletedEvent eventMessage)
        {
            // 1. Calculate and persist the immutable rules
            await _commissionEvaluator.CalculateSplitsAsync(eventMessage.NativeOrderId);

            // 2. Fetch the calculated splits to snapshot into Escrow
            var splits = await _splitRepository.GetAllAsync(q => q.Where(s => s.NativeOrderId == eventMessage.NativeOrderId));

            // 3. Initialize the Escrow Container with exact financial snapshots
            var escrow = new EscrowTransaction
            {
                CoreOrderId = eventMessage.NativeOrderId,
                CurrentStateId = (int)EscrowState.Created,

                GrossAmount = splits.Sum(s => s.CustomerPaidAmount),
                GatewayFeeAmount = splits.Sum(s => s.GatewayFeeAmount),
                PlatformFeeAmount = splits.Sum(s => s.PlatformFeeAmount),
                NetSupplierAmount = splits.Sum(s => s.SupplierWholesaleAmount),
                NetResellerAmount = splits.Sum(s => s.ResellerMarginAmount),

                UpdatedOnUtc = DateTime.UtcNow
            };

            await _escrowRepository.InsertAsync(escrow);

            // 4. Audit Trail
            await _historyRepository.InsertAsync(new EscrowStateHistory
            {
                EscrowTransactionId = escrow.Id,
                OldStateId = 0,
                NewStateId = (int)EscrowState.Created,
                SystemNote = "Escrow Initialized. Financial splits locked.",
                CreatedOnUtc = DateTime.UtcNow
            });
        }
    }
}