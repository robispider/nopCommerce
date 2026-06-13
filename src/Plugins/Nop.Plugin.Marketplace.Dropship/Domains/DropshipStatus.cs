namespace Nop.Plugin.Marketplace.Dropship.Domains
{
    public enum DropshipStatus
    {
        Pending = 10,
        AwaitingResellerDeposit = 15, // <--- NEW: Procurement Lock
        Accepted = 20,
        Shipped = 30,
        Delivered = 40,
        Cancelled = 50
    }
}