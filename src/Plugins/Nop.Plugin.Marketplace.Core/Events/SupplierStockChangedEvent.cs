namespace Nop.Plugin.Marketplace.Core.Events
{
    /// <summary>
    /// Fired when a Supplier's inventory is altered, signaling Reseller catalogs to sync.
    /// </summary>
    public class SupplierStockChangedEvent
    {
        public int SupplierProductId { get; set; }
        public int NewStockQuantity { get; set; }
        public DateTime TimestampUtc { get; set; }

        public SupplierStockChangedEvent(int supplierProductId, int newStockQuantity)
        {
            SupplierProductId = supplierProductId;
            NewStockQuantity = newStockQuantity;
            TimestampUtc = DateTime.UtcNow;
        }
    }
}