using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Inventory.Domains
{
    public partial class StockReservation : BaseEntity
    {
        public int InventoryBucketId { get; set; }
        public int OrderItemId { get; set; } // Nullable if tracking cart items before order creation, but strictly OrderItem post-creation
        public int QuantityReserved { get; set; }

        /// <summary>
        /// 15-minute TTL. If not confirmed by this time, background task releases it.
        /// </summary>
        public DateTime? ExpiresOnUtc { get; set; }

        public int StatusId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime? ReleasedOnUtc { get; set; }
    }
}