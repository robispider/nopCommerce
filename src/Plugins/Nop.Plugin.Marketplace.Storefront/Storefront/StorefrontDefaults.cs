using Nop.Core.Caching;

namespace Nop.Plugin.Marketplace.Storefront
{
    public static class StorefrontDefaults
    {
        /// <summary>
        /// Gets a key for caching a storefront by its URL slug.
        /// {0} : UrlSlug
        /// </summary>
        // FIX: Removed the second argument. The modern CacheKey constructor only takes the key pattern.
        public static CacheKey StorefrontBySlugCacheKey => new("Marketplace.Storefront.byslug.{0}");

        /// <summary>
        /// Gets a prefix pattern to clear all storefront cache when a record is updated.
        /// </summary>
        public static string StorefrontPrefixCacheKey => "Marketplace.Storefront.";
    }
}