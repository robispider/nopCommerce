namespace Nop.Plugin.Marketplace.Core.Domains
{
    public class CommissionSplitResult
    {
        public decimal TotalOrderAmount { get; set; }

        // The Gateway processing fee (absorbed by Platform)
        public decimal GatewayFeeAmount { get; set; }

        public int SupplierVendorId { get; set; }
        public decimal SupplierAmount { get; set; }

        public int ResellerVendorId { get; set; }
        public decimal ResellerAmount { get; set; }

        // What the platform actually keeps
        public decimal NetPlatformFeeAmount { get; set; }
    }
}