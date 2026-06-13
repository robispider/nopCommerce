namespace Nop.Plugin.Marketplace.Order.Events
{
    public class OrderSplitCompletedEvent
    {
        public int MarketplaceOrderGroupId { get; set; }
        public int NativeOrderId { get; set; }
        public decimal TotalAmount { get; set; }
    }
}