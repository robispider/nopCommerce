namespace Nop.Plugin.Marketplace.Commission.Domains.Enums
{
    /// <summary>
    /// The strict evaluation matrix. Lower number = Higher Priority.
    /// </summary>
    public enum CommissionPriority
    {
        SkuOverride = 10,       // Highest: Negotiated rate for a specific product
        VendorOverride = 20,    // Vendor has a special platform-wide rate
        CategoryOverride = 30,  // Electronics (3%), Clothing (8%)
        TierOverride = 40,      // Volume discount (e.g., sold > $10k this month)
        GlobalDefault = 100     // Lowest: The standard platform fee (e.g., 5%)
    }
}