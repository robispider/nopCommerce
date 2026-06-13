using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Marketplace.Dropship.Domains;
using Nop.Plugin.Marketplace.Dropship.Models;
using Nop.Plugin.Marketplace.Dropship.Services;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Marketplace.Dropship.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [AutoValidateAntiforgeryToken]
    public class SupplierDropshipController : BasePluginController
    {
        private readonly IDropshipFulfillmentService _dropshipService;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IWorkContext _workContext;

        public SupplierDropshipController(
            IDropshipFulfillmentService dropshipService,
            IOrderService orderService,
            IProductService productService,
            IPriceFormatter priceFormatter,
            IWorkContext workContext)
        {
            _dropshipService = dropshipService;
            _orderService = orderService;
            _productService = productService;
            _priceFormatter = priceFormatter;
            _workContext = workContext;
        }

        public async Task<IActionResult> List()
        {
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return AccessDeniedView();

            var model = new DropshipTicketSearchModel();
            model.SetGridPageSize();

            return View("~/Plugins/Marketplace.Dropship/Views/SupplierDropship/List.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> List(DropshipTicketSearchModel searchModel)
        {
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return Json(new DropshipTicketListModel());

            var tickets = await _dropshipService.SearchSupplierTicketsAsync(vendor.Id, searchModel.Page - 1, searchModel.PageSize);

            var model = new DropshipTicketListModel().PrepareToGrid(searchModel, tickets, () =>
            {
                return tickets.Select(t =>
                {
                    var orderItem = _orderService.GetOrderItemByIdAsync(t.OrderItemId).Result;
                    var product = _productService.GetProductByIdAsync(orderItem?.ProductId ?? 0).Result;
                    var order = _orderService.GetOrderByIdAsync(t.OrderId).Result;

                    string statusHtml = t.DropshipStatusId == (int)DropshipStatus.Shipped
                        ? "<span class='badge bg-success'>Shipped</span>"
                        : "<span class='badge bg-warning'>Pending</span>";

                    return new DropshipTicketModel
                    {
                        Id = t.Id,
                        OrderNumber = order?.CustomOrderNumber ?? t.OrderId.ToString(),
                        ProductName = product?.Name ?? "Unknown Product",
                        Quantity = orderItem?.Quantity ?? 0,
                        LockedWholesalePrice = _priceFormatter.FormatPriceAsync(t.LockedWholesalePrice).Result,
                        StatusHtml = statusHtml,
                        TrackingNumber = string.IsNullOrEmpty(t.TrackingNumber) ? "Not Shipped" : t.TrackingNumber
                    };
                });
            });

            return Json(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTracking(int id, string trackingNumber)
        {
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return Json(new { success = false, message = "Not authorized." });

            var ticket = await _dropshipService.GetByIdAsync(id);
            if (ticket == null || ticket.SupplierVendorId != vendor.Id)
                return Json(new { success = false, message = "Ticket not found." });

            ticket.TrackingNumber = trackingNumber;
            ticket.DropshipStatusId = (int)DropshipStatus.Shipped;
            ticket.ShippedOnUtc = DateTime.UtcNow;

            await _dropshipService.UpdateFulfillmentAsync(ticket);

            return Json(new { success = true });
        }
    }
}