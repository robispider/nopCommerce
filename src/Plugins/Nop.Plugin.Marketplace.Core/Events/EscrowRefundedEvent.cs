namespace Nop.Plugin.Marketplace.Core.Events
{
    public class EscrowRefundedEvent
    {
        public int EscrowTransactionId { get; set; }
        public int ResellerVendorId { get; set; }
        public decimal RefundAmount { get; set; }
        public string IdempotencyKey { get; set; }
    }
}