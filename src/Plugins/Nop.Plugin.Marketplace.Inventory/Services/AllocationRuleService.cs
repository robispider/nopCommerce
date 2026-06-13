using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains; // For ResellerProductMapping
using Nop.Plugin.Marketplace.Inventory.Domains;
using Nop.Plugin.Marketplace.Inventory.Domains.Enums;

namespace Nop.Plugin.Marketplace.Inventory.Services
{
    public class AllocationRuleService : IAllocationRuleService
    {
        private readonly IRepository<InventoryBucket> _bucketRepository;
        private readonly IRepository<ResellerProductMapping> _mappingRepository;

        public AllocationRuleService(
            IRepository<InventoryBucket> bucketRepository,
            IRepository<ResellerProductMapping> mappingRepository)
        {
            _bucketRepository = bucketRepository;
            _mappingRepository = mappingRepository;
        }

        public async Task<InventoryBucket> DetermineFulfillmentBucketAsync(int productId, int? resellerVendorId, int requiredQuantity)
        {
            // 1. If it's a direct supplier sale (no reseller), just pull from the Supplier bucket.
            if (!resellerVendorId.HasValue || resellerVendorId.Value == 0)
            {
                var buckets = await _bucketRepository.GetAllAsync(q => q.Where(b =>
                    b.ProductId == productId &&
                    b.BucketTypeId == (int)InventoryBucketType.SupplierStock));

                return buckets.FirstOrDefault();
            }

            // 2. It's a Reseller sale. Find the mapping to locate the supplier.
            var mappings = await _mappingRepository.GetAllAsync(q => q.Where(m =>
                m.ResellerCoreProductId == productId &&
                m.ResellerBusinessId == resellerVendorId.Value));

            var mapping = mappings.FirstOrDefault();

            if (mapping == null)
                throw new Exception($"No ResellerProductMapping found for Product {productId} and Reseller {resellerVendorId}");

            // 3. Check Reseller's own inventory bucket first (Policy: ResellerFirst)
            var resellerBuckets = await _bucketRepository.GetAllAsync(q => q.Where(b =>
                b.ProductId == productId &&
                b.SourceVendorId == resellerVendorId.Value &&
                b.BucketTypeId == (int)InventoryBucketType.ResellerInventory));

            var resellerBucket = resellerBuckets.FirstOrDefault();

            if (resellerBucket != null && resellerBucket.AvailableQuantity >= requiredQuantity)
            {
                return resellerBucket; // Reseller has enough stock, use theirs.
            }

            // 4. Fallback to Supplier's inventory bucket (Dropship)
            var supplierBuckets = await _bucketRepository.GetAllAsync(q => q.Where(b =>
                b.ProductId == mapping.SupplierCoreProductId &&
                b.BucketTypeId == (int)InventoryBucketType.SupplierStock));

            var supplierBucket = supplierBuckets.FirstOrDefault();

            if (supplierBucket != null && supplierBucket.AvailableQuantity >= requiredQuantity)
            {
                return supplierBucket; // Supplier has enough stock.
            }

            // 5. If we reach here, neither has enough stock. Return the supplier bucket so the reservation service can fail and trigger Backorder logic (if enabled).
            return supplierBucket ?? resellerBucket;
        }
    }
}