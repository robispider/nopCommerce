namespace Nop.Plugin.Marketplace.Core.Domains
{
    public enum EscrowState
    {
        Created = 10,       // Waiting for payment to clear
        Funded = 30,        // Money is locked. Supplier can ship.
        Processing = 50,    // Supplier acknowledged
        Shipped = 70,       // Tracking uploaded
        Delivered = 90,     // Courier confirmed
        GracePeriod = 110,   // 72-hour countdown for disputes
        SettlementPending = 130, // NEW: Escrow approved it, waiting for Wallet to confirm
        Settled = 150,           // NEW (Replaces Released): Wallet confirmed the credit
        AwaitingCustomerConfirmation = 160,
        Disputed = 170,      // Terminal (Pending Admin): Consumer raised issue
        Refunded = 190,      // Terminal: Money returned to consumer
        Cancelled = 210     // Terminal: Order failed/cancelled before shipping
    }
}
