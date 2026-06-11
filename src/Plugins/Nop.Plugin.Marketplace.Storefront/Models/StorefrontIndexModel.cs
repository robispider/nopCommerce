using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Marketplace.Storefront.Models
{
    public record StorefrontIndexModel
    {
        public string StoreName { get; init; }
        public string BannerUrl { get; init; }
        public IList<ProductOverviewModel> Products { get; init; } = new List<ProductOverviewModel>();
    }
}