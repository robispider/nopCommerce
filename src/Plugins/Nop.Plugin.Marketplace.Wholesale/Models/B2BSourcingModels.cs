using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Marketplace.Wholesale.Models
{
    public record B2BProductSearchModel : BaseSearchModel { }

    public record B2BProductListModel : BasePagedListModel<B2BProductModel> { }

    public record B2BProductModel : BaseNopEntityModel
    {
        public int ProductId { get; set; }
        public string PictureThumbnailUrl { get; set; }

        [NopResourceDisplayName("Product Name")]
        public string ProductName { get; set; }

        [NopResourceDisplayName("Supplier Name")]
        public string SupplierName { get; set; }

        [NopResourceDisplayName("Wholesale Price")]
        public string WholesalePrice { get; set; }

        [NopResourceDisplayName("MOQ")]
        public int MinimumOrderQuantity { get; set; }

        public bool IsDropshipEnabled { get; set; }
        public bool IsPreorderEnabled { get; set; }

        // Formatted strings for the DataTables grid badges
        [NopResourceDisplayName("Availability")]
        public string BadgesHtml { get; set; }
    }
}