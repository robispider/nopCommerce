using System;
using System.Threading.Tasks;

using Nop.Core.Caching;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Escrow.Domains;
using Nop.Plugin.Marketplace.Escrow.Services;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Escrow.Events
{
    public class CustomerDisputedOrderEventConsumer : IConsumer<CustomerDisputedOrderEvent>
    {
        private readonly IRepository<EscrowTransaction> _escrowRepository;
        private readonly IEscrowService _escrowService;
        private readonly ILocker _locker;

        public CustomerDisputedOrderEventConsumer(
            IRepository<EscrowTransaction> escrowRepository,
            IEscrowService escrowService,
            ILocker locker)
        {
            _escrowRepository = escrowRepository;
            _escrowService = escrowService;
            _locker = locker;
        }

        public async Task HandleEventAsync(CustomerDisputedOrderEvent eventMessage)
        {
            string lockKey = $"marketplace_escrow_dispute_lock_{eventMessage.NativeOrderId}";

            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
            {
                var escrow = await _escrowRepository.Table
                    .FirstOrDefaultAsync(e => e.CoreOrderId == eventMessage.NativeOrderId);

                if (escrow != null)
                {
                    // The IEscrowService already contains safe logic to reject disputes 
                    // if the state is already Settled or Refunded.
                    await _escrowService.DisputeEscrowAsync(
                        escrow.Id,
                        $"CUSTOMER DISPUTE: {eventMessage.Reason}",
                        eventMessage.CustomerId // Logging who raised it
                    );
                }
            });
        }
    }
}