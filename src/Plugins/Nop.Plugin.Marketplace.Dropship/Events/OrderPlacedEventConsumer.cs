using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Data; // Required for IRepository
using Nop.Plugin.Marketplace.Core.Domains; // Required for ResellerProductMapping & ProcurementSettlementPolicy
using Nop.Plugin.Marketplace.Dropship.Domains;
using Nop.Plugin.Marketplace.Dropship.Services;
using Nop.Plugin.Marketplace.Wholesale.Services;
using Nop.Services.Catalog;
using Nop.Services.Events;
using Nop.Services.Orders;

namespace Nop.Plugin.Marketplace.Dropship.Events
{
    /// <summary>
    /// Listens for native B2C checkouts and splits dropship items into Supplier tickets.
    /// Evaluates Reseller Procurement Policies before sending to Supplier.
    /// </summary>
    public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ISupplierProductService _supplierProductService;
        private readonly IDropshipFulfillmentService _dropshipFulfillmentService;
        private readonly IRepository<ResellerProductMapping> _mappingRepository;

        public OrderPlacedEventConsumer(
            IOrderService orderService,
            IProductService productService,
            ISupplierProductService supplierProductService,
            IDropshipFulfillmentService dropshipFulfillmentService,
            IRepository<ResellerProductMapping> mappingRepository)
        {
            _orderService = orderService;
            _productService = productService;
            _supplierProductService = supplierProductService;
            _dropshipFulfillmentService = dropshipFulfillmentService;
            _mappingRepository = mappingRepository;
        }

        public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
        {
            var order = eventMessage.Order;

            // 1. Get the items the consumer just bought
            var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

            foreach (var item in orderItems)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                if (product == null)
                    continue;

                var resellerVendorId = product.VendorId;

                // 2. Determine if this product is a Dropship clone.
                // Because we used "Light Cloning" in Phase 1, the ParentGroupedProductId 
                // holds the original Supplier's Product ID. (Or alternatively, a mapped attribute).
                int sourceProductId = product.ParentGroupedProductId > 0 ? product.ParentGroupedProductId : product.Id;

                var supplierB2BSettings = await _supplierProductService.GetByProductIdAsync(sourceProductId);

                // 3. If it's a valid dropship product, generate the Ticket!
                if (supplierB2BSettings != null && supplierB2BSettings.IsDropshipEnabled && resellerVendorId != supplierB2BSettings.VendorId)
                {
                    // Find the Reseller Mapping to see what Procurement Policy they agreed to when importing
                    var resellerMapping = _mappingRepository.Table
                        .FirstOrDefault(m => m.ResellerCoreProductId == product.Id);

                    int policyId = resellerMapping?.SelectedProcurementPolicyId ?? (int)ProcurementSettlementPolicy.FullEscrow;
                    int initialStatus = (int)DropshipStatus.Pending; // Default

                    // THE PROCUREMENT GATEWAY
                    if (policyId == (int)ProcurementSettlementPolicy.ResellerPrepay)
                    {
                        // The reseller must fund this out of pocket before the supplier acts!
                        initialStatus = (int)DropshipStatus.AwaitingResellerDeposit;
                    }

                    var fulfillment = new DropshipFulfillment
                    {
                        OrderId = order.Id,
                        OrderItemId = item.Id,
                        ResellerVendorId = resellerVendorId,
                        SupplierVendorId = supplierB2BSettings.VendorId,

                        // Lock in the financials to protect the Reseller from sudden price changes!
                        LockedWholesalePrice = supplierB2BSettings.WholesalePrice * item.Quantity,
                        LockedRetailPrice = item.PriceExclTax, // What the consumer paid natively

                        DropshipStatusId = initialStatus,              // <--- Injected status
                        ProcurementPolicyId = policyId,                // <--- Logging the policy
                        CreatedOnUtc = DateTime.UtcNow
                    };

                    await _dropshipFulfillmentService.InsertFulfillmentAsync(fulfillment);
                }
            }
        }
    }
}