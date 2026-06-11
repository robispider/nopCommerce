using Nop.Core;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Services.Catalog;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Business.Services
{
    public class MarketplaceCatalogService : IMarketplaceCatalogService
    {
        private readonly ICopyProductService _copyProductService;
        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<ResellerProductMapping> _mappingRepository;
        private readonly IEventPublisher _eventPublisher;

        public MarketplaceCatalogService(
            ICopyProductService copyProductService,
            IProductService productService,
            IWorkContext workContext,
            IRepository<ResellerProductMapping> mappingRepository,
            IEventPublisher eventPublisher)
        {
            _copyProductService = copyProductService;
            _productService = productService;
            _workContext = workContext;
            _mappingRepository = mappingRepository;
            _eventPublisher = eventPublisher;
        }

        public async Task ImportSupplierProductAsync(int supplierProductId, decimal retailMarginPercentage)
        {
            var currentVendor = await _workContext.GetCurrentVendorAsync();
            if (currentVendor == null)
                throw new Exception("Only authenticated vendors can import products.");

            var supplierProduct = await _productService.GetProductByIdAsync(supplierProductId);
            if (supplierProduct == null)
                throw new Exception("Supplier product not found.");

  
            // 1. Use Native nopCommerce Clone Service
            // This copies attributes, pictures, and categories safely.
            var clonedProduct = await _copyProductService.CopyProductAsync(
                product: supplierProduct,
                newName: supplierProduct.Name,
                isPublished: true,
                copyMultimedia: true,             // <--- UPDATED PARAMETER NAME
                copyAssociatedProducts: false);

            // 2. Reassign Ownership to the Reseller & Apply Margin
            clonedProduct.VendorId = currentVendor.Id;
            clonedProduct.Price = supplierProduct.Price * (1 + (retailMarginPercentage / 100m));

            // If the supplier has no stock, ensure the clone starts out as unavailable
            clonedProduct.DisableBuyButton = supplierProduct.StockQuantity <= 0;

            await _productService.UpdateProductAsync(clonedProduct);

            // 3. Create the Mapping Record in our Plugin Table
            var mapping = new ResellerProductMapping
            {
                ResellerCoreProductId = clonedProduct.Id,
                SupplierCoreProductId = supplierProductId,
                ResellerBusinessId = currentVendor.Id,
                SyncInventory = true,
                MarginPercentage = retailMarginPercentage,
                CreatedOnUtc = DateTime.UtcNow
            };
            await _mappingRepository.InsertAsync(mapping);

            // 4. Publish Domain Event across the system
            await _eventPublisher.PublishAsync(new ProductClonedEvent(
                supplierProductId,
                clonedProduct.Id,
                currentVendor.Id,
                retailMarginPercentage));
        }
    }
}