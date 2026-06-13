using System;
using System.Threading.Tasks;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Marketplace.Inventory.Services;
using Nop.Plugin.Marketplace.Order.Domains;
using Nop.Plugin.Marketplace.Order.Domains.Enums;
using Nop.Plugin.Marketplace.Order.Events;
using Nop.Services.Catalog;
using Nop.Services.Events;
using Nop.Services.Orders;

namespace Nop.Plugin.Marketplace.Order.Services
{
    public class OrderAllocationService : IOrderAllocationService
    {
        private readonly IRepository<MarketplaceOrderGroup> _orderGroupRepository;
        private readonly IRepository<MarketplaceOrderAllocation> _allocationRepository;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly IInventoryReservationService _inventoryService;
        private readonly IEventPublisher _eventPublisher;

        public OrderAllocationService(
            IRepository<MarketplaceOrderGroup> orderGroupRepository,
            IRepository<MarketplaceOrderAllocation> allocationRepository,
            IOrderService orderService,
            IProductService productService,
            IInventoryReservationService inventoryService,
            IEventPublisher eventPublisher)
        {
            _orderGroupRepository = orderGroupRepository;
            _allocationRepository = allocationRepository;
            _orderService = orderService;
            _productService = productService;
            _inventoryService = inventoryService;
            _eventPublisher = eventPublisher;
        }

        public async Task<MarketplaceOrderGroup> SplitNativeOrderAsync(Nop.Core.Domain.Orders.Order nativeOrder)
        {
            // 1. Create the Group Container
            var group = new MarketplaceOrderGroup
            {
                NativeOrderId = nativeOrder.Id,
                TotalAmount = nativeOrder.OrderTotal,
                StatusId = (int)MarketplaceOrderStatus.Created,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await _orderGroupRepository.InsertAsync(group);

            var items = await _orderService.GetOrderItemsAsync(nativeOrder.Id);

            foreach (var item in items)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                if (product == null)
                    continue;

                var resellerVendorId = product.VendorId > 0 ? (int?)product.VendorId : null;

                // 2. Firmly Reserve Inventory (Removes it from Available stock atomically)
                // If this fails due to concurrency, it throws an exception, rolling back the transaction natively.
                var reservation = await _inventoryService.ReserveStockAsync(
                    product.Id,
                    resellerVendorId,
                    item.Id,
                    item.Quantity,
                    expiryMinutes: 0); // 0 means firm reservation immediately

                await _inventoryService.ConfirmReservationAsync(reservation.Id);

                // 3. Create the Allocation line
                // In a highly advanced setup, FulfillmentMethod is determined by the AllocationRuleService.
                // For MVP, if it has a Reseller, it's Dropship. If direct, Standard.
                var allocation = new MarketplaceOrderAllocation
                {
                    MarketplaceOrderGroupId = group.Id,
                    VendorId = resellerVendorId ?? 0,
                    OrderItemId = item.Id,
                    AllocatedAmount = item.PriceExclTax,
                    FulfillmentMethodId = resellerVendorId.HasValue ? (int)FulfillmentMethod.Dropship : (int)FulfillmentMethod.StandardShipping,
                    StatusId = 10, // Pending
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow
                };

                await _allocationRepository.InsertAsync(allocation);
            }

            // 4. Update Status to Allocated
            group.StatusId = (int)MarketplaceOrderStatus.Allocated;
            await _orderGroupRepository.UpdateAsync(group);

            // 5. Publish Event for Dropship & Escrow plugins to catch!
            await _eventPublisher.PublishAsync(new OrderSplitCompletedEvent
            {
                MarketplaceOrderGroupId = group.Id,
                NativeOrderId = nativeOrder.Id,
                TotalAmount = group.TotalAmount
            });

            return group;
        }
    }
}