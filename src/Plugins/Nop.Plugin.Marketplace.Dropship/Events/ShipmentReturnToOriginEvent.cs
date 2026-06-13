using System;

namespace Nop.Plugin.Marketplace.Core.Events
{
    public class ShipmentReturnToOriginEvent
    {
        public int DropshipFulfillmentId { get; set; }
        public int NativeOrderId { get; set; }
        public string TrackingNumber { get; set; }
        public string Reason { get; set; } // e.g., "Customer Refused", "Address Invalid"
        public DateTime ReturnedOnUtc { get; set; }
    }
}