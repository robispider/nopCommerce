namespace Nop.Plugin.Marketplace.Core.Events
{
    public class ReserveHoldRequestedEvent
    {
        public int VendorId { get; set; }
        public decimal Amount { get; set; }
        public int EscrowTransactionId { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public class ReserveReleasedEvent
    {
        public int VendorId { get; set; }
        public decimal Amount { get; set; }
        public int ReserveScheduleId { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public class ChargebackDeductedEvent
    {
        public int VendorId { get; set; }
        public decimal Amount { get; set; }
        public int CoreOrderId { get; set; }
        public string IdempotencyKey { get; set; }
    }
}