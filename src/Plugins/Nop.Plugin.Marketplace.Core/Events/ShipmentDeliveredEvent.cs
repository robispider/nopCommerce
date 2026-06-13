using System;

namespace Nop.Plugin.Marketplace.Core.Events
{
    public class ShipmentDeliveredEvent
    {
        public int DropshipFulfillmentId { get; set; }
        public int NativeOrderId { get; set; }
        public string TrackingNumber { get; set; }
        public string CourierSystemName { get; set; }
        public DateTime DeliveredOnUtc { get; set; }
    }
}