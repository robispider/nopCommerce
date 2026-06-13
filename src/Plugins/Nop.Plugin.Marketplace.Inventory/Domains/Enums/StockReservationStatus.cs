namespace Nop.Plugin.Marketplace.Inventory.Domains.Enums
{
    public enum StockReservationStatus
    {
        Active = 10,    // Soft reservation during checkout
        Confirmed = 20, // Firm reservation (paid)
        Released = 30,  // Cancelled or TTL expired
        Fulfilled = 40  // Shipped out
    }
}