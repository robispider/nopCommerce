namespace Nop.Plugin.Marketplace.Core.Events
{
    public class WithdrawalApprovedEvent
    {
        public int WithdrawalId { get; set; }
        public int VendorId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } // e.g. "Bank Transfer", "Wire"
    }
}