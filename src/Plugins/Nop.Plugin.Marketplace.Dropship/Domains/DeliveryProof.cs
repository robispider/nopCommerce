using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Dropship.Domains
{
    /// <summary>
    /// The unarguable evidence that a package reached its destination.
    /// Vital for fighting chargebacks and resolving vendor/customer disputes.
    /// </summary>
    public class DeliveryProof : BaseEntity
    {
        public int DropshipFulfillmentId { get; set; }

        public int ConfirmationSourceId { get; set; } // Maps to DeliveryConfirmationSource

        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }

        public string SignaturePictureUrl { get; set; } // Signed manifest
        public string ProofPictureUrl { get; set; }     // Photo of package at door

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public string SystemNote { get; set; } // e.g., "FedEx Webhook EventID: 998877"

        public DateTime DeliveredOnUtc { get; set; }
    }
}