using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Events;
using Nop.Plugin.Marketplace.Core.Events; // <-- We use Events now instead of Domains!
using Nop.Services.Events;                // <-- NopCommerce Event Publisher
using Nop.Services.Orders;
using Nop.Web.Controllers;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Marketplace.Order.Controllers
{
    /// <summary>
    /// B2C Front-End Controller. Allows the actual buyer to verify delivery.
    /// </summary>
    public class CustomerOrderVerificationController : BasePublicController
    {
        private readonly IWorkContext _workContext;
        private readonly IOrderService _orderService;
        private readonly IEventPublisher _eventPublisher; // <-- Replaced IEscrowService!

        public CustomerOrderVerificationController(
            IWorkContext workContext,
            IOrderService orderService,
            IEventPublisher eventPublisher) // <-- Replaced IEscrowService!
        {
            _workContext = workContext;
            _orderService = orderService;
            _eventPublisher = eventPublisher;
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmReceipt(int orderId)
        {
            var customer = await _workContext.GetCurrentCustomerAsync();
            var order = await _orderService.GetOrderByIdAsync(orderId);

            if (order == null || order.CustomerId != customer.Id)
                return Unauthorized();

            try
            {
                // Customer explicitly clicks "Order Received". 
                // We publish an event. We DO NOT talk to Escrow directly!
                // This breaks the Circular Dependency.
                await _eventPublisher.PublishAsync(new CustomerConfirmedReceiptEvent
                {
                    NativeOrderId = order.Id,
                    CustomerId = customer.Id
                });

                return Json(new { success = true, message = "Thank you for confirming your delivery!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}