using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Plugin.Marketplace.Inventory.Domains;
using Nop.Plugin.Marketplace.Inventory.Domains.Enums;

namespace Nop.Plugin.Marketplace.Inventory.Services
{
    public class InventoryBucketService : IInventoryBucketService
    {
        private readonly IRepository<InventoryBucket> _bucketRepository;

        public InventoryBucketService(IRepository<InventoryBucket> bucketRepository)
        {
            _bucketRepository = bucketRepository;
        }

        public async Task<InventoryBucket> GetBucketAsync(int productId, int? vendorId, InventoryBucketType type)
        {
            // Fix for CS1503: Pass the query logic as a Func delegate instead of a raw IQueryable
            var buckets = await _bucketRepository.GetAllAsync(query =>
            {
                query = query.Where(b => b.ProductId == productId && b.BucketTypeId == (int)type);

                if (vendorId.HasValue)
                    query = query.Where(b => b.SourceVendorId == vendorId.Value);
                else
                    query = query.Where(b => !b.SourceVendorId.HasValue);

                return query;
            });

            return buckets.FirstOrDefault();
        }

        public async Task InsertBucketAsync(InventoryBucket bucket)
        {
            if (bucket == null)
                throw new ArgumentNullException(nameof(bucket));
            bucket.UpdatedOnUtc = DateTime.UtcNow;
            bucket.ConcurrencyVersion = 1;
            await _bucketRepository.InsertAsync(bucket);
        }

        public async Task UpdateBucketAsync(InventoryBucket bucket)
        {
            if (bucket == null)
                throw new ArgumentNullException(nameof(bucket));

            bucket.UpdatedOnUtc = DateTime.UtcNow;
            bucket.ConcurrencyVersion++; // Manual optimistic concurrency increment

            await _bucketRepository.UpdateAsync(bucket);
        }

        public async Task AdjustAvailableStockAsync(int bucketId, int quantityDelta)
        {
            var bucket = await _bucketRepository.GetByIdAsync(bucketId);
            if (bucket == null)
                return;

            bucket.AvailableQuantity += quantityDelta;

            // Enforce constraint in code instead of raw SQL for DB agnosticism
            if (bucket.AvailableQuantity < 0)
                bucket.AvailableQuantity = 0;

            await UpdateBucketAsync(bucket);
        }
    }
}