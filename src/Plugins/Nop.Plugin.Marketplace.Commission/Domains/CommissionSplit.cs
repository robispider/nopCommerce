using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Commission.Domains
{
    /// <summary>
    /// Immutable audit record of exactly how the money was distributed for a specific order item.
    /// </summary>
    public partial class CommissionSplit : BaseEntity
    {
        public int NativeOrderId { get; set; }
        public int OrderItemId { get; set; }

        public int AppliedBaseRuleId { get; set; }
        public string AppliedModifierRuleIds { get; set; } // Comma separated IDs if stacked

        public decimal CustomerPaidAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GatewayFeeAmount { get; set; }
        public decimal PlatformFeeAmount { get; set; }

        public int SupplierVendorId { get; set; }
        public decimal SupplierWholesaleAmount { get; set; }

        public int? ResellerVendorId { get; set; }
        public decimal ResellerMarginAmount { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }
}