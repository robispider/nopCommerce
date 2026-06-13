using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Marketplace.Wholesale.Models
{
    public record SourcedProductSearchModel : BaseSearchModel { }

    public record SourcedProductListModel : BasePagedListModel<SourcedProductModel> { }

    public record SourcedProductModel : BaseNopEntityModel
    {
        public string PictureThumbnailUrl { get; set; }

        [NopResourceDisplayName("Product (Retail Name)")]
        public string RetailProductName { get; set; }

        [NopResourceDisplayName("Supplier")]
        public string SupplierName { get; set; }

        [NopResourceDisplayName("Wholesale Cost (Locked)")]
        public string WholesaleCost { get; set; }

        [NopResourceDisplayName("Your Retail Price")]
        public string RetailPrice { get; set; }

        [NopResourceDisplayName("Margin")]
        public string MarginHtml { get; set; }

        [NopResourceDisplayName("Procurement Policy")]
        public string ProcurementPolicyHtml { get; set; }
    }
}