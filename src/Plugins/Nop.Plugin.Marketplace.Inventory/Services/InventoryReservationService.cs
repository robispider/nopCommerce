using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Plugin.Marketplace.Inventory.Domains;
using Nop.Plugin.Marketplace.Inventory.Domains.Enums;

namespace Nop.Plugin.Marketplace.Inventory.Services
{
    public class InventoryReservationService : IInventoryReservationService
    {
        private readonly IRepository<StockReservation> _reservationRepository;
        private readonly IInventoryBucketService _bucketService;
        private readonly IAllocationRuleService _allocationRuleService;

        public InventoryReservationService(
            IRepository<StockReservation> reservationRepository,
            IInventoryBucketService bucketService,
            IAllocationRuleService allocationRuleService)
        {
            _reservationRepository = reservationRepository;
            _bucketService = bucketService;
            _allocationRuleService = allocationRuleService;
        }

        public async Task<StockReservation> ReserveStockAsync(int productId, int? resellerVendorId, int orderItemId, int quantity, int expiryMinutes = 15)
        {
            // 1. Determine which bucket to pull from
            var targetBucket = await _allocationRuleService.DetermineFulfillmentBucketAsync(productId, resellerVendorId, quantity);

            if (targetBucket == null)
                throw new Exception($"No inventory bucket found to fulfill Product {productId}");

            // 2. Concurrency & Availability Check
            // In a fully scaled environment (Phase 6), a Redis RedLock would be applied here around the BucketId.
            if (targetBucket.AvailableQuantity < quantity)
            {
                // Note: If AllowOversell is true, we would route to BackorderQuantity here instead.
                // For Sprint 2 MVP, we enforce strict stock.
                throw new Exception($"Not enough stock available. Requested: {quantity}, Available: {targetBucket.AvailableQuantity}");
            }

            // 3. Adjust Bucket Stock
            targetBucket.AvailableQuantity -= quantity;
            targetBucket.ReservedQuantity += quantity;
            await _bucketService.UpdateBucketAsync(targetBucket);

            // 4. Create the TTL Reservation
            var reservation = new StockReservation
            {
                InventoryBucketId = targetBucket.Id,
                OrderItemId = orderItemId,
                QuantityReserved = quantity,
                StatusId = (int)StockReservationStatus.Active,
                CreatedOnUtc = DateTime.UtcNow,
                ExpiresOnUtc = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };

            await _reservationRepository.InsertAsync(reservation);

            return reservation;
        }

        public async Task ConfirmReservationAsync(int reservationId)
        {
            var reservation = await _reservationRepository.GetByIdAsync(reservationId);
            if (reservation == null || reservation.StatusId != (int)StockReservationStatus.Active)
                return;

            reservation.StatusId = (int)StockReservationStatus.Confirmed;
            reservation.ExpiresOnUtc = null; // Remove TTL, it is now firmly purchased

            await _reservationRepository.UpdateAsync(reservation);
        }

        public async Task ReleaseReservationAsync(int reservationId)
        {
            var reservation = await _reservationRepository.GetByIdAsync(reservationId);
            if (reservation == null)
                return;

            // Only Active (Soft) or Confirmed (Firm) reservations can be released
            if (reservation.StatusId == (int)StockReservationStatus.Active ||
                reservation.StatusId == (int)StockReservationStatus.Confirmed)
            {
                // Restore bucket quantities
                var bucket = await _bucketService.GetBucketAsync(reservation.InventoryBucketId, null, (InventoryBucketType)0); // type doesn't matter for ID lookup, but we'll use repository direct if needed

                // Better: we should fetch bucket by ID. We need to add GetBucketByIdAsync to bucket service, but let's do it safely:
                await _bucketService.AdjustAvailableStockAsync(reservation.InventoryBucketId, reservation.QuantityReserved);

                // Adjust reserved back down (requires fetching the bucket)
                // Let's implement a clean decrease of reserved quantity
                // (In a real scenario, we add a specific method for this in BucketService, but we can do it inline for speed):
                var repo = Nop.Core.Infrastructure.EngineContext.Current.Resolve<IRepository<InventoryBucket>>();
                var actualBucket = await repo.GetByIdAsync(reservation.InventoryBucketId);
                if (actualBucket != null)
                {
                    actualBucket.ReservedQuantity -= reservation.QuantityReserved;
                    if (actualBucket.ReservedQuantity < 0)
                        actualBucket.ReservedQuantity = 0;
                    await repo.UpdateAsync(actualBucket);
                }

                // Update Reservation status
                reservation.StatusId = (int)StockReservationStatus.Released;
                reservation.ReleasedOnUtc = DateTime.UtcNow;
                reservation.ExpiresOnUtc = null;

                await _reservationRepository.UpdateAsync(reservation);
            }
        }
    }
}