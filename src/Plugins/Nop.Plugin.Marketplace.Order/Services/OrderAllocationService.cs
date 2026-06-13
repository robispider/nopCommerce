using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
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
        private readonly IRepository<ResellerProductMapping> _mappingRepository; // Inject mapping
        private readonly IOrderService _orderService; // Inject order service for notes
        private readonly IProductService _productService;
        private readonly IInventoryReservationService _inventoryService;
        private readonly IEventPublisher _eventPublisher;

        public OrderAllocationService(
            IRepository<MarketplaceOrderGroup> orderGroupRepository,
            IRepository<MarketplaceOrderAllocation> allocationRepository,
            IRepository<ResellerProductMapping> mappingRepository,
            IOrderService orderService,
            IProductService productService,
            IInventoryReservationService inventoryService,
            IEventPublisher eventPublisher)
        {
            _orderGroupRepository = orderGroupRepository;
            _allocationRepository = allocationRepository;
            _mappingRepository = mappingRepository;
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

            var temporaryReservations = new System.Collections.Generic.List<int>();
            bool allocationSuccess = true;
            string failureReason = string.Empty;

            try
            {
                // PHASE 1: Soft-Reserve all items first (Two-Phase Commit)
                foreach (var item in items)
                {
                    var product = await _productService.GetProductByIdAsync(item.ProductId);
                    if (product == null)
                        continue;

                    var resellerVendorId = product.VendorId > 0 ? (int?)product.VendorId : null;
                    int authoritativeProductId = product.Id;

                    // FIX 1: Resolve the authoritative Supplier product ID if it's a reseller clone
                    if (resellerVendorId.HasValue)
                    {
                        var mappingQuery = await _mappingRepository.GetAllAsync(query =>
                            query.Where(m => m.ResellerCoreProductId == product.Id));

                        var mapping = mappingQuery.FirstOrDefault();
                        if (mapping != null)
                        {
                            authoritativeProductId = mapping.SupplierCoreProductId;
                        }
                    }

                    // Soft reservation with a 15-minute TTL
                    var reservation = await _inventoryService.ReserveStockAsync(
                        authoritativeProductId,
                        resellerVendorId,
                        item.Id,
                        item.Quantity,
                        expiryMinutes: 15);

                    temporaryReservations.Add(reservation.Id);
                }

                // PHASE 2: All soft reservations succeeded! Now firmly CONFIRM them.
                foreach (var resId in temporaryReservations)
                {
                    await _inventoryService.ConfirmReservationAsync(resId);
                }
            }
            catch (Exception ex)
            {
                allocationSuccess = false;
                failureReason = ex.Message;

                // ROLLBACK: Release any successfully created soft-reservations to prevent ghost leaks
                foreach (var resId in temporaryReservations)
                {
                    await _inventoryService.ReleaseReservationAsync(resId);
                }
            }

            // FIX 2: Handle post-checkout stock failures gracefully
            if (!allocationSuccess)
            {
                group.StatusId = (int)MarketplaceOrderStatus.Cancelled;
                await _orderGroupRepository.UpdateAsync(group);

                // Add an internal audit note to the native order so the merchant is immediately alerted
                await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = nativeOrder.Id,
                    Note = $"MARKETPLACE CRITICAL ERROR: Multi-vendor inventory allocation failed. Reason: {failureReason}. Order placed on hold.",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                return group;
            }

            // PHASE 3: Write allocations only after successful stock verification
            foreach (var item in items)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                var resellerVendorId = product?.VendorId > 0 ? (int?)product.VendorId : null;

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

            group.StatusId = (int)MarketplaceOrderStatus.Allocated;
            await _orderGroupRepository.UpdateAsync(group);

            // Publish completed splits to Dropship, Escrow & Commission engines
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