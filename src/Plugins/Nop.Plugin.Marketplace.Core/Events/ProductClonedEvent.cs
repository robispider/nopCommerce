namespace Nop.Plugin.Marketplace.Core.Events
{
    /// <summary>
    /// Fired when a Reseller successfully imports a Supplier product to their catalog.
    /// </summary>
    public class ProductClonedEvent
    {
        public int SupplierProductId { get; set; }
        public int ResellerProductId { get; set; }
        public int ResellerVendorId { get; set; }
        public decimal AppliedMarginPercentage { get; set; }

        public ProductClonedEvent(int supplierProductId, int resellerProductId, int resellerVendorId, decimal appliedMarginPercentage)
        {
            SupplierProductId = supplierProductId;
            ResellerProductId = resellerProductId;
            ResellerVendorId = resellerVendorId;
            AppliedMarginPercentage = appliedMarginPercentage;
        }
    }
}