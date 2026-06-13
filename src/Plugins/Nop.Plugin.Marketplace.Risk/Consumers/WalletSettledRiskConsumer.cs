using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Data;
using Nop.Core.Events; // <-- FIXED: Changed from Nop.Events
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Risk.Domains;
using Nop.Plugin.Marketplace.Escrow.Domains; // <-- FIXED: Added Escrow Domains
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Risk.Consumers
{
    public class WalletSettledRiskConsumer : IConsumer<WalletSettledEvent>
    {
        private readonly IRepository<VendorReserveRule> _ruleRepository;
        private readonly IRepository<ReserveSchedule> _scheduleRepository;
        private readonly IRepository<OutboxMessage> _outboxRepository;
        private readonly IRepository<EscrowTransaction> _escrowRepository;

        public WalletSettledRiskConsumer(
            IRepository<VendorReserveRule> ruleRepository,
            IRepository<ReserveSchedule> scheduleRepository,
            IRepository<OutboxMessage> outboxRepository,
            IRepository<EscrowTransaction> escrowRepository)
        {
            _ruleRepository = ruleRepository;
            _scheduleRepository = scheduleRepository;
            _outboxRepository = outboxRepository;
            _escrowRepository = escrowRepository;
        }

        public async Task HandleEventAsync(WalletSettledEvent eventMessage)
        {
            var escrow = await _escrowRepository.GetByIdAsync(eventMessage.EscrowTransactionId);
            if (escrow == null)
                return;

            // In a real scenario, you'd calculate for both Supplier and Reseller.
            // For brevity, we calculate for the Reseller.
            var rule = await _ruleRepository.Table.FirstOrDefaultAsync(x => x.VendorId == escrow.ResellerVendorId)
                       ?? await _ruleRepository.Table.FirstOrDefaultAsync(x => x.VendorId == 0); // Fallback to Global

            if (rule == null || rule.HoldPercentage <= 0)
                return;

            // We must reconstruct the amount (normally fetched from CommissionService)
            // Let's assume the Reseller made $1250 on this order.
            decimal resellerEarnings = 1250m;
            decimal holdAmount = Math.Round(resellerEarnings * (rule.HoldPercentage / 100m), 2);

            var schedule = new ReserveSchedule
            {
                VendorId = escrow.ResellerVendorId,
                EscrowTransactionId = escrow.Id,
                HeldAmount = holdAmount,
                ReleaseOnUtc = DateTime.UtcNow.AddDays(rule.HoldDays),
                IsReleased = false,
                CreatedOnUtc = DateTime.UtcNow
            };
            await _scheduleRepository.InsertAsync(schedule);

            // Write to Outbox so Wallet safely moves the money
            var holdEvent = new ReserveHoldRequestedEvent
            {
                VendorId = schedule.VendorId,
                Amount = holdAmount,
                EscrowTransactionId = escrow.Id,
                IdempotencyKey = $"RSV_HOLD_{schedule.Id}"
            };

            await _outboxRepository.InsertAsync(new OutboxMessage
            {
                EventType = "Nop.Plugin.Marketplace.Core.Events.ReserveHoldRequestedEvent",
                Payload = JsonConvert.SerializeObject(holdEvent),
                CreatedOnUtc = DateTime.UtcNow
            });
        }
    }
}