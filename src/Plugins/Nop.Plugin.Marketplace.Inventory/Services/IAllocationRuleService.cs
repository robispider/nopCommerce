using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Inventory.Domains;

namespace Nop.Plugin.Marketplace.Inventory.Services
{
    public interface IAllocationRuleService
    {
        /// <summary>
        /// Determines the correct inventory bucket to pull stock from based on the reseller's configuration and current stock levels.
        /// </summary>
        Task<InventoryBucket> DetermineFulfillmentBucketAsync(int productId, int? resellerVendorId, int requiredQuantity);
    }
}