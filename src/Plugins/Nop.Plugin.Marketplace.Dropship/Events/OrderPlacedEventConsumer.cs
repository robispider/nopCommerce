using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Dropship.Domains;
using Nop.Plugin.Marketplace.Dropship.Services;
using Nop.Plugin.Marketplace.Order.Domains; // <-- Access to Allocations
using Nop.Plugin.Marketplace.Order.Domains.Enums; // <-- Access to FulfillmentMethod
using Nop.Plugin.Marketplace.Order.Events; // <-- Access to the Event
using Nop.Plugin.Marketplace.Wholesale.Services;
using Nop.Services.Catalog;
using Nop.Services.Events;
using Nop.Services.Orders;

namespace Nop.Plugin.Marketplace.Dropship.Events
{
    /// <summary>
    /// Listens for the Order Split completion from the Marketplace.Order plugin.
    /// It grabs any allocations marked for "Dropship" and generates Supplier tickets.
    /// </summary>
    public class OrderSplitCompletedEventConsumer : IConsumer<OrderSplitCompletedEvent>
    {
        private readonly IRepository<MarketplaceOrderAllocation> _allocationRepository;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ISupplierProductService _supplierProductService;
        private readonly IDropshipFulfillmentService _dropshipFulfillmentService;
        private readonly IRepository<ResellerProductMapping> _mappingRepository;

        public OrderSplitCompletedEventConsumer(
            IRepository<MarketplaceOrderAllocation> allocationRepository, // Inject new repository
            IOrderService orderService,
            IProductService productService,
            ISupplierProductService supplierProductService,
            IDropshipFulfillmentService dropshipFulfillmentService,
            IRepository<ResellerProductMapping> mappingRepository)
        {
            _allocationRepository = allocationRepository;
            _orderService = orderService;
            _productService = productService;
            _supplierProductService = supplierProductService;
            _dropshipFulfillmentService = dropshipFulfillmentService;
            _mappingRepository = mappingRepository;
        }

        // FIXED SIGNATURE: Now correctly matches the event type
        public async Task HandleEventAsync(OrderSplitCompletedEvent eventMessage)
        {
            // 1. Fetch only the allocations for this order that were routed to Dropship
            var dropshipAllocations = await _allocationRepository.GetAllAsync(query =>
                query.Where(a =>
                    a.MarketplaceOrderGroupId == eventMessage.MarketplaceOrderGroupId &&
                    a.FulfillmentMethodId == (int)FulfillmentMethod.Dropship));

            foreach (var allocation in dropshipAllocations)
            {
                // We still need the native order item to get the quantity and product mapping
                var item = await _orderService.GetOrderItemByIdAsync(allocation.OrderItemId);
                if (item == null)
                    continue;

                var product = await _productService.GetProductByIdAsync(item.ProductId);
                if (product == null)
                    continue;

                int sourceProductId = product.ParentGroupedProductId > 0 ? product.ParentGroupedProductId : product.Id;
                var supplierB2BSettings = await _supplierProductService.GetByProductIdAsync(sourceProductId);

                if (supplierB2BSettings != null)
                {
                    // Find the Reseller Mapping to see what Procurement Policy they agreed to when importing
                    var resellerMapping = await _mappingRepository.GetAllAsync(query =>
                        query.Where(m => m.ResellerCoreProductId == product.Id));
                    var mapping = resellerMapping.FirstOrDefault();

                    int policyId = mapping?.SelectedProcurementPolicyId ?? (int)ProcurementSettlementPolicy.FullEscrow;
                    int initialStatus = (int)DropshipStatus.Pending;

                    // THE PROCUREMENT GATEWAY
                    if (policyId == (int)ProcurementSettlementPolicy.ResellerPrepay)
                    {
                        initialStatus = (int)DropshipStatus.AwaitingResellerDeposit;
                    }

                    var fulfillment = new DropshipFulfillment
                    {
                        OrderId = eventMessage.NativeOrderId,
                        OrderItemId = item.Id,
                        ResellerVendorId = product.VendorId,
                        SupplierVendorId = supplierB2BSettings.VendorId,

                        // Lock in the financials to protect the Reseller from sudden price changes!
                        LockedWholesalePrice = supplierB2BSettings.WholesalePrice * item.Quantity,
                        LockedRetailPrice = item.PriceExclTax,

                        DropshipStatusId = initialStatus,
                        ProcurementPolicyId = policyId,
                        CreatedOnUtc = DateTime.UtcNow
                    };

                    await _dropshipFulfillmentService.InsertFulfillmentAsync(fulfillment);
                }
            }
        }
    }
}