namespace Nop.Plugin.Marketplace.Dropship.Domains
{
    public enum DeliveryConfirmationSource
    {
        CourierWebhook = 10,
        SupplierManual = 20,     // "Own Driver" / "Self Delivery"
        CustomerConfirmed = 30,  // Customer clicked "I received this"
        AdminVerified = 40,      // Platform Admin forced delivery
        AutoConfirmed = 50       // 7-day auto-timer expired
    }
}