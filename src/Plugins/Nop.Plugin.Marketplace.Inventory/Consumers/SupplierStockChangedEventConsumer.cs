using System.Threading.Tasks;
using Nop.Services.Events;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Inventory.Services;
using Nop.Plugin.Marketplace.Inventory.Domains.Enums;

namespace Nop.Plugin.Marketplace.Inventory.Consumers
{
    public class SupplierStockChangedEventConsumer : IConsumer<SupplierStockChangedEvent>
    {
        private readonly IInventoryBucketService _inventoryBucketService;

        public SupplierStockChangedEventConsumer(IInventoryBucketService inventoryBucketService)
        {
            _inventoryBucketService = inventoryBucketService;
        }

        public async Task HandleEventAsync(SupplierStockChangedEvent eventMessage)
        {
            // We find the supplier bucket using the SupplierProductId. 
            // Assuming Supplier VendorId isn't needed if ProductId is globally unique for suppliers.
            var bucket = await _inventoryBucketService.GetBucketAsync(
                eventMessage.SupplierProductId,
                null, // Passing null or looking up vendor if required
                InventoryBucketType.SupplierStock);

            if (bucket != null)
            {
                // Set the exact quantity since the native system represents the source of truth
                bucket.AvailableQuantity = eventMessage.NewStockQuantity;
                await _inventoryBucketService.UpdateBucketAsync(bucket);
            }
        }
    }
}