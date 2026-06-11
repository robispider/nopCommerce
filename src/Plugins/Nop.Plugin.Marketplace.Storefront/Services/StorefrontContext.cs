using Microsoft.AspNetCore.Http;
using Nop.Core.Caching;
using Nop.Data;
using Nop.Plugin.Marketplace.Storefront.Domains;

namespace Nop.Plugin.Marketplace.Storefront.Services
{
    public class StorefrontContext : IStorefrontContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<ResellerStorefront> _storefrontRepository;
        private readonly IStaticCacheManager _staticCacheManager;

        // Tier 1 Cache: Memory for the lifecycle of a single HTTP request
        private ResellerStorefront _cachedStorefront;
        private bool _hasEvaluated;

        public StorefrontContext(
            IHttpContextAccessor httpContextAccessor,
            IRepository<ResellerStorefront> storefrontRepository,
            IStaticCacheManager staticCacheManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _storefrontRepository = storefrontRepository;
            _staticCacheManager = staticCacheManager;
        }

        public async Task<ResellerStorefront> GetCurrentStorefrontAsync()
        {
            // If we already figured it out during this page load, return it instantly!
            if (_hasEvaluated)
                return _cachedStorefront;

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return null;

            string slug = null;

            // URL Sniffer: Looks for "yourdomain.com/store/{slug}"
            var path = request.Path.Value;
            if (!string.IsNullOrEmpty(path) && path.StartsWith("/store/", StringComparison.OrdinalIgnoreCase))
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 2 && segments[0].Equals("store", StringComparison.OrdinalIgnoreCase))
                {
                    slug = segments[1].ToLowerInvariant(); // e.g., "shoeking"
                }
            }

            // (Optional Future Hook: Check request.Host here for Custom Domains like "shoeking.com")

            if (string.IsNullOrEmpty(slug))
            {
                _hasEvaluated = true;
                return null;
            }

            // Tier 2 Cache: Query Redis / Database
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(StorefrontDefaults.StorefrontBySlugCacheKey, slug);

            _cachedStorefront = await _staticCacheManager.GetAsync(cacheKey, async () =>
            {
                return await _storefrontRepository.Table
                    .FirstOrDefaultAsync(x => x.UrlSlug.ToLower() == slug && x.IsActive);
            });

            _hasEvaluated = true;
            return _cachedStorefront;
        }
    }
}