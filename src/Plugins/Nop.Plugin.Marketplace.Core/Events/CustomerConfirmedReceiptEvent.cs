namespace Nop.Plugin.Marketplace.Core.Events
{
    public class CustomerConfirmedReceiptEvent
    {
        public int NativeOrderId { get; set; }
        public int CustomerId { get; set; }
    }
}