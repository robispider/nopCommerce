using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Caching; // <-- NopCommerce Native Distributed Locking
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Dropship.Domains;
using Nop.Plugin.Marketplace.Escrow.Services;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Escrow.Events
{
    public class ShipmentDeliveredEventConsumer : IConsumer<ShipmentDeliveredEvent>
    {
        private readonly IRepository<DropshipFulfillment> _fulfillmentRepository;
        private readonly IEscrowService _escrowService;
        private readonly ILocker _locker;

        public ShipmentDeliveredEventConsumer(
            IRepository<DropshipFulfillment> fulfillmentRepository,
            IEscrowService escrowService,
            ILocker locker)
        {
            _fulfillmentRepository = fulfillmentRepository;
            _escrowService = escrowService;
            _locker = locker;
        }

        public async Task HandleEventAsync(ShipmentDeliveredEvent eventMessage)
        {
            string lockKey = $"marketplace_escrow_delivery_lock_{eventMessage.NativeOrderId}";

            // ILocker guarantees only ONE thread across all your web servers 
            // can process this specific Order ID at a time.
            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
            {
                // 1. Fetch all tickets for the order (Fresh from DB)
                var allOrderTickets = await _fulfillmentRepository.Table
                    .Where(t => t.OrderId == eventMessage.NativeOrderId)
                    .ToListAsync();

                // 2. Check if the entire B2C order is complete
                // A ticket is considered complete if Delivered OR Cancelled
                bool isEntireOrderComplete = allOrderTickets.All(t =>
                    t.DropshipStatusId == (int)DropshipStatus.Delivered ||
                    t.DropshipStatusId == (int)DropshipStatus.Cancelled);

                if (isEntireOrderComplete)
                {
                    // 3. The IEscrowService implementation will check if it is already
                    // past the Shipped state. If Thread B enters this lock after Thread A, 
                    // TransitionStateByOrderIdAsync will naturally ignore it.
                    await _escrowService.TransitionStateByOrderIdAsync(
                        eventMessage.NativeOrderId,
                        EscrowState.AwaitingCustomerConfirmation, // ALIBABA MODEL
                        $"All supplier tickets fulfilled. Awaiting customer confirmation before starting grace period."
                    );
                }
            });
        }
    }
}