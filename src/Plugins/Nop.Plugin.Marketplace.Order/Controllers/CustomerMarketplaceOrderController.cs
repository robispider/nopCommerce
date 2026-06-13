using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Events;
using Nop.Plugin.Marketplace.Core.Events; // <-- Uses Event instead of Escrow logic
using Nop.Services.Events;
using Nop.Services.Orders;
using Nop.Web.Controllers;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Marketplace.Order.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class CustomerMarketplaceOrderController : BasePublicController
    {
        private readonly IWorkContext _workContext;
        private readonly IOrderService _orderService;
        private readonly IEventPublisher _eventPublisher;

        public CustomerMarketplaceOrderController(
            IWorkContext workContext,
            IOrderService orderService,
            IEventPublisher eventPublisher)
        {
            _workContext = workContext;
            _orderService = orderService;
            _eventPublisher = eventPublisher;
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmReceipt(int orderId)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer == null)
                return Challenge();

            var order = await _orderService.GetOrderByIdAsync(orderId);

            if (order == null || order.CustomerId != customer.Id)
                return Unauthorized();

            try
            {
                // Decoupled Event Publication! Breaks the circular dependency.
                await _eventPublisher.PublishAsync(new CustomerConfirmedReceiptEvent
                {
                    NativeOrderId = order.Id,
                    CustomerId = customer.Id
                });

                return Json(new { success = true, message = "Thank you! Your delivery has been confirmed." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Failed to confirm receipt." });
            }
        }
        [HttpPost]
        public async Task<IActionResult> DisputeOrder(int orderId, string reason)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            if (customer == null)
                return Challenge();

            var order = await _orderService.GetOrderByIdAsync(orderId);

            // Security check
            if (order == null || order.CustomerId != customer.Id)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(reason))
                return Json(new { success = false, message = "You must provide a reason for the dispute." });

            try
            {
                // Shout into the ecosystem! Escrow will hear this and freeze the funds.
                await _eventPublisher.PublishAsync(new CustomerDisputedOrderEvent
                {
                    NativeOrderId = order.Id,
                    CustomerId = customer.Id,
                    Reason = reason
                });

                return Json(new { success = true, message = "Dispute raised successfully. Our team will review this shortly." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Failed to raise dispute." });
            }
        }
    }
}