using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Order.Domains
{
    public partial class MarketplaceOrderAllocation : BaseEntity
    {
        public int MarketplaceOrderGroupId { get; set; }
        public int VendorId { get; set; } // The vendor responsible for fulfillment
        public int OrderItemId { get; set; } // Map to native line item
        public decimal AllocatedAmount { get; set; }
        public int FulfillmentMethodId { get; set; }
        public int StatusId { get; set; } // 10=Pending, 20=Confirmed
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
    }
}