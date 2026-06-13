using System;
using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Escrow.Services;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Escrow.Events
{
    public class ShipmentReturnToOriginConsumer : IConsumer<ShipmentReturnToOriginEvent>
    {
        private readonly IEscrowService _escrowService;
        private readonly ILocker _locker;

        public ShipmentReturnToOriginConsumer(
            IEscrowService escrowService,
            ILocker locker)
        {
            _escrowService = escrowService;
            _locker = locker;
        }

        public async Task HandleEventAsync(ShipmentReturnToOriginEvent eventMessage)
        {
            string lockKey = $"marketplace_escrow_rto_lock_{eventMessage.NativeOrderId}";

            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
            {
                // Move Escrow to Refunded. 
                // This will automatically trigger your Wallet to refund the Reseller's deposit/hold
                // and block the Supplier from receiving wholesale earnings.
                await _escrowService.TransitionStateByOrderIdAsync(
                    eventMessage.NativeOrderId,
                    EscrowState.Refunded,
                    $"Delivery failed (RTO). Tracking: {eventMessage.TrackingNumber}. Reason: {eventMessage.Reason}. Escrow reversed."
                );
            });
        }
    }
}