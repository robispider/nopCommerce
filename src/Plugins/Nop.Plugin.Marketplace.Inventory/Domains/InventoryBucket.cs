using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Inventory.Domains
{
    public partial class InventoryBucket : BaseEntity
    {
        public int ProductId { get; set; }
        public int? SourceVendorId { get; set; }
        public int BucketTypeId { get; set; }

        public int AvailableQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int BackorderQuantity { get; set; }

        public int ConcurrencyVersion { get; set; }

        public DateTime UpdatedOnUtc { get; set; }
    }
}