using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Marketplace.Dropship.Models
{
    public record DropshipTicketSearchModel : BaseSearchModel { }

    public record DropshipTicketListModel : BasePagedListModel<DropshipTicketModel> { }

    public record DropshipTicketModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Order #")]
        public string OrderNumber { get; set; }

        [NopResourceDisplayName("Product")]
        public string ProductName { get; set; }

        [NopResourceDisplayName("Qty")]
        public int Quantity { get; set; }

        [NopResourceDisplayName("Wholesale Earnings")]
        public string LockedWholesalePrice { get; set; }

        [NopResourceDisplayName("Status")]
        public string StatusHtml { get; set; }

        [NopResourceDisplayName("Tracking Number")]
        public string TrackingNumber { get; set; }
    }
}