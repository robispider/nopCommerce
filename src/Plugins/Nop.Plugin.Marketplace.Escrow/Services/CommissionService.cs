using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Services.Orders;

namespace Nop.Plugin.Marketplace.Escrow.Services
{
    public class CommissionService : ICommissionService
    {
        private readonly IOrderService _orderService;

        public CommissionService(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<CommissionSplitResult> CalculateSplitsAsync(int coreOrderId)
        {
            var order = await _orderService.GetOrderByIdAsync(coreOrderId);
            if (order == null)
                throw new Exception("Order not found");

            // For enterprise grade, we fetch the actual order items.
            var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

            // NOTE: In a real environment, you would query your Marketplace.Dropship tables here
            // to find the exact Wholesale Cost agreed upon at the time of checkout.
            // For this implementation, we will use standard calculations:

            decimal totalPaid = order.OrderTotal;

            // Let's assume a hard rule for now: 
            // - Gateway fee is 1.5%
            // - Platform takes 5% of the total order
            // - Supplier gets 75% of the remaining 
            // - Reseller gets 25% of the remaining
            // (You can connect this to a DB settings table later)

            decimal gatewayFee = Math.Round(totalPaid * 0.015m, 2);
            decimal grossPlatformFee = Math.Round(totalPaid * 0.05m, 2);

            decimal netAvailableToVendors = totalPaid - grossPlatformFee;

            // In Phase 4, you linked the child order to the specific vendors.
            // We'll mock the IDs for the split logic until hooked into the Dropship plugin.
            decimal supplierCut = Math.Round(netAvailableToVendors * 0.75m, 2);
            decimal resellerCut = Math.Round(netAvailableToVendors * 0.25m, 2);

            // Handle rounding pennies
            decimal roundingDifference = netAvailableToVendors - (supplierCut + resellerCut);
            resellerCut += roundingDifference;

            return new CommissionSplitResult
            {
                TotalOrderAmount = totalPaid,
                GatewayFeeAmount = gatewayFee,
                NetPlatformFeeAmount = grossPlatformFee - gatewayFee,

                SupplierVendorId = 0, // TODO: Pull from Order/Dropship mapping
                SupplierAmount = supplierCut,

                ResellerVendorId = 0, // TODO: Pull from Order/Reseller mapping
                ResellerAmount = resellerCut
            };
        }
    }
}