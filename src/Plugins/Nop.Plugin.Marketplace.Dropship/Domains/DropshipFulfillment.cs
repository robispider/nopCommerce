using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Dropship.Domains
{
    public partial class DropshipFulfillment : BaseEntity
    {
        public int OrderId { get; set; }          // The B2C Customer's Native Order
        public int OrderItemId { get; set; }      // The specific item in the cart
        public int ResellerVendorId { get; set; } // The storefront owner
        public int SupplierVendorId { get; set; } // The B2B supplier
                                                  // Add this line to the class
        public int ProcurementPolicyId { get; set; }

        public decimal LockedWholesalePrice { get; set; } // Supplier gets this
        public decimal LockedRetailPrice { get; set; }    // Reseller gets (Retail - Wholesale)

        public int DropshipStatusId { get; set; } // 10=Pending, 20=Accepted, 30=Shipped

        public string TrackingNumber { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime? ShippedOnUtc { get; set; }

        public string CourierSystemName { get; set; } // "fedex", "pathao", "self-delivery"
        public DateTime? DeliveredOnUtc { get; set; }
    }
}