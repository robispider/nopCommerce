using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Marketplace.Commission.Services; // Inject Commission split reader
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Escrow.Domains;
using Nop.Plugin.Marketplace.Risk.Domains;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Risk.Consumers
{
    public class WalletSettledRiskConsumer : IConsumer<WalletSettledEvent>
    {
        private readonly IRepository<VendorReserveRule> _ruleRepository;
        private readonly IRepository<ReserveSchedule> _scheduleRepository;
        private readonly IRepository<EscrowTransaction> _escrowRepository;
        private readonly ICommissionEvaluatorService _commissionService;
        private readonly IEventPublisher _eventPublisher;

        public WalletSettledRiskConsumer(
            IRepository<VendorReserveRule> ruleRepository,
            IRepository<ReserveSchedule> scheduleRepository,
            IRepository<EscrowTransaction> escrowRepository,
            ICommissionEvaluatorService commissionService,
            IEventPublisher eventPublisher)
        {
            _ruleRepository = ruleRepository;
            _scheduleRepository = scheduleRepository;
            _escrowRepository = escrowRepository;
            _commissionService = commissionService;
            _eventPublisher = eventPublisher;
        }

        public async Task HandleEventAsync(WalletSettledEvent eventMessage)
        {
            var escrow = await _escrowRepository.GetByIdAsync(eventMessage.EscrowTransactionId);
            if (escrow == null)
                return;

            // ALIBABA-GRADE: Fetch the actual split results instead of using hardcoded assumptions
            var splits = await _commissionService.GetExistingSplitsAsync(escrow.CoreOrderId);

            // 1. Process Reserve Hold for the Supplier
            await EvaluateAndApplyHoldAsync(splits.SupplierVendorId, splits.SupplierAmount, escrow.Id);

            // 2. Process Reserve Hold for the Reseller (if applicable)
            if (splits.ResellerVendorId > 0 && splits.ResellerAmount > 0)
            {
                await EvaluateAndApplyHoldAsync(splits.ResellerVendorId, splits.ResellerAmount, escrow.Id);
            }
        }

        private async Task EvaluateAndApplyHoldAsync(int vendorId, decimal netEarnings, int escrowTransactionId)
        {
            var rule = await _ruleRepository.Table.FirstOrDefaultAsync(x => x.VendorId == vendorId)
                       ?? await _ruleRepository.Table.FirstOrDefaultAsync(x => x.VendorId == 0); // Global Fallback

            if (rule == null || rule.HoldPercentage <= 0)
                return;

            decimal holdAmount = Math.Round(netEarnings * (rule.HoldPercentage / 100m), 2);
            if (holdAmount <= 0)
                return;

            var schedule = new ReserveSchedule
            {
                VendorId = vendorId,
                EscrowTransactionId = escrowTransactionId,
                HeldAmount = holdAmount,
                ReleaseOnUtc = DateTime.UtcNow.AddDays(rule.HoldDays),
                IsReleased = false,
                CreatedOnUtc = DateTime.UtcNow
            };
            await _scheduleRepository.InsertAsync(schedule);

            // Directly publish event to lock-free Wallet transaction queue
            await _eventPublisher.PublishAsync(new ReserveHoldRequestedEvent
            {
                VendorId = schedule.VendorId,
                Amount = holdAmount,
                EscrowTransactionId = escrowTransactionId,
                IdempotencyKey = $"RSV_HOLD_{schedule.Id}"
            });
        }
    }
}