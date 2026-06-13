using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Order.Domains
{
    public partial class MarketplaceOrderGroup : BaseEntity
    {
        public int NativeOrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public int StatusId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public DateTime? CompletedOnUtc { get; set; }
        public int ConcurrencyVersion { get; set; }
    }
}