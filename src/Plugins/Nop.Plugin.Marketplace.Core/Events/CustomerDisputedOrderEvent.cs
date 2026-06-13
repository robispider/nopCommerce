namespace Nop.Plugin.Marketplace.Core.Events
{
    public class CustomerDisputedOrderEvent
    {
        public int NativeOrderId { get; set; }
        public int CustomerId { get; set; }
        public string Reason { get; set; }
    }
}