using Nop.Core.Domain.Catalog;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Services.Catalog;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Business.Events
{
    /// <summary>
    /// Intercepts native product updates. If the product belongs to a supplier,
    /// it syncs the inventory and calculated price to all reseller clones.
    /// </summary>
    public class SupplierProductUpdatedConsumer : IConsumer<EntityUpdatedEvent<Product>>
    {
        private readonly IRepository<ResellerProductMapping> _mappingRepository;
        private readonly IProductService _productService;

        public SupplierProductUpdatedConsumer(
            IRepository<ResellerProductMapping> mappingRepository,
            IProductService productService)
        {
            _mappingRepository = mappingRepository;
            _productService = productService;
        }

        public async Task HandleEventAsync(EntityUpdatedEvent<Product> eventMessage)
        {
            var updatedProduct = eventMessage.Entity;

            // 1. Find all active Reseller mappings tied to this updated product
            // (If the updated product is a reseller's clone, this query safely returns empty and breaks the loop)
            var mappings = _mappingRepository.Table
                .Where(x => x.SupplierCoreProductId == updatedProduct.Id && x.SyncInventory)
                .ToList();

            if (!mappings.Any())
                return;

            // 2. Loop through and sync all clones
            foreach (var map in mappings)
            {
                var resellerProduct = await _productService.GetProductByIdAsync(map.ResellerCoreProductId);
                if (resellerProduct != null)
                {
                    bool requiresUpdate = false;

                    // Sync Stock
                    if (resellerProduct.StockQuantity != updatedProduct.StockQuantity)
                    {
                        resellerProduct.StockQuantity = updatedProduct.StockQuantity;
                        requiresUpdate = true;
                    }

                    // Sync Price based on the Reseller's specific margin rule
                    var calculatedRetailPrice = updatedProduct.Price * (1 + (map.MarginPercentage / 100m));
                    if (resellerProduct.Price != calculatedRetailPrice)
                    {
                        resellerProduct.Price = calculatedRetailPrice;
                        requiresUpdate = true;
                    }

                    // Handle Out of Stock auto-disabling
                    bool shouldDisable = updatedProduct.StockQuantity <= 0;
                    if (resellerProduct.DisableBuyButton != shouldDisable)
                    {
                        resellerProduct.DisableBuyButton = shouldDisable;
                        requiresUpdate = true;
                    }

                    // 3. Save changes if necessary
                    if (requiresUpdate)
                    {
                        await _productService.UpdateProductAsync(resellerProduct);
                    }
                }
            }
        }
    }
}