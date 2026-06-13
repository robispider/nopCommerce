// Models/SupplierProductModels.cs
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Marketplace.Wholesale.Models
{
    public record SupplierProductSearchModel : BaseSearchModel { }

    public record SupplierProductListModel : BasePagedListModel<SupplierProductModel> { }

    public record SupplierProductModel : BaseNopEntityModel
    {
        public int ProductId { get; set; }

        [NopResourceDisplayName("Product Name")]
        public string ProductName { get; set; }

        [NopResourceDisplayName("Wholesale Price")]
        public decimal WholesalePrice { get; set; }

        [NopResourceDisplayName("MOQ")]
        public int MinimumOrderQuantity { get; set; }

        [NopResourceDisplayName("Dropship Enabled")]
        public bool IsDropshipEnabled { get; set; }

        [NopResourceDisplayName("Preorder Enabled")]
        public bool IsPreorderEnabled { get; set; }

        [NopResourceDisplayName("Lead Time (Days)")]
        public int LeadTimeDays { get; set; }

        [NopResourceDisplayName("Allowed Procurement Policies")]
        public int AllowedProcurementPolicies { get; set; } // Bitwise integer
    }
}