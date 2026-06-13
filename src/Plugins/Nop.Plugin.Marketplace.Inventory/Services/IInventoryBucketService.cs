using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Inventory.Domains;
using Nop.Plugin.Marketplace.Inventory.Domains.Enums;

namespace Nop.Plugin.Marketplace.Inventory.Services
{
    public interface IInventoryBucketService
    {
        Task<InventoryBucket> GetBucketAsync(int productId, int? vendorId, InventoryBucketType type);
        Task InsertBucketAsync(InventoryBucket bucket);
        Task UpdateBucketAsync(InventoryBucket bucket);

        /// <summary>
        /// Forces a raw stock adjustment (used heavily by sync events).
        /// </summary>
        Task AdjustAvailableStockAsync(int bucketId, int quantityDelta);
    }
}