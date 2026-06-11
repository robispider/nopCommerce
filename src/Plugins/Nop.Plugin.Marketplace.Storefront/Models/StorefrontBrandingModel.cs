namespace Nop.Plugin.Marketplace.Storefront.Models
{
    public record StorefrontBrandingModel
    {
        public string StoreName { get; init; }
        public string PrimaryColorHex { get; init; }
        public string LogoUrl { get; init; }
    }
}