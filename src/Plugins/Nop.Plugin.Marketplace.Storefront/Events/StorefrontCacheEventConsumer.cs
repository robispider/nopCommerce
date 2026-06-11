using Nop.Core.Events;
using Nop.Core.Caching;
using Nop.Plugin.Marketplace.Storefront.Domains;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Storefront.Events
{
    /// <summary>
    /// Intercepts native EF Core database changes to ResellerStorefront and automatically flushes the Redis cache.
    /// </summary>
    public class StorefrontCacheEventConsumer :
        IConsumer<EntityInsertedEvent<ResellerStorefront>>,
        IConsumer<EntityUpdatedEvent<ResellerStorefront>>,
        IConsumer<EntityDeletedEvent<ResellerStorefront>>
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public StorefrontCacheEventConsumer(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager;
        }

        public async Task HandleEventAsync(EntityInsertedEvent<ResellerStorefront> eventMessage)
            => await ClearCacheAsync();

        public async Task HandleEventAsync(EntityUpdatedEvent<ResellerStorefront> eventMessage)
            => await ClearCacheAsync();

        public async Task HandleEventAsync(EntityDeletedEvent<ResellerStorefront> eventMessage)
            => await ClearCacheAsync();

        private async Task ClearCacheAsync()
        {
            // Flushes all cache keys starting with "Marketplace.Storefront."
            await _staticCacheManager.RemoveByPrefixAsync(StorefrontDefaults.StorefrontPrefixCacheKey);
        }
    }
}