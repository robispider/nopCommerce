using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Escrow.Domains
{
    public partial class EscrowTransaction : BaseEntity
    {
        public int CoreOrderId { get; set; }
        public int CurrentStateId { get; set; }

        // Backward-compatibility properties
        public int SupplierVendorId { get; set; }
        public int ResellerVendorId { get; set; }

        // Elite Financial Snapshots
        public decimal GrossAmount { get; set; }
        public decimal GatewayFeeAmount { get; set; }
        public decimal PlatformFeeAmount { get; set; }
        public decimal NetSupplierAmount { get; set; }
        public decimal NetResellerAmount { get; set; }

        public DateTime UpdatedOnUtc { get; set; }
        public int ConcurrencyVersion { get; set; }
    }
}