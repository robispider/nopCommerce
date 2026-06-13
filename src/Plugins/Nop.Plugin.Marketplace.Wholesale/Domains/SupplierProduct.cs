using Nop.Core;

namespace Nop.Plugin.Marketplace.Wholesale.Domains
{
    /// <summary>
    /// Represents the B2B wholesale rules for a specific native nopCommerce product.
    /// </summary>
    public partial class SupplierProduct : BaseEntity
    {
        public int ProductId { get; set; }
        public int VendorId { get; set; }
        public int AllowedProcurementPolicies { get; set; }
        public decimal WholesalePrice { get; set; }
        public int MinimumOrderQuantity { get; set; }
        public bool IsDropshipEnabled { get; set; }
        public bool IsPreorderEnabled { get; set; }
        public int LeadTimeDays { get; set; }
    }
}