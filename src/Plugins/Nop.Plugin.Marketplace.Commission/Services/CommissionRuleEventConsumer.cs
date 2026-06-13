using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Core.Events;
using Nop.Plugin.Marketplace.Commission.Domains;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Commission.Services
{
    public class CommissionRuleEventConsumer :
        IConsumer<EntityInsertedEvent<CommissionRule>>,
        IConsumer<EntityUpdatedEvent<CommissionRule>>,
        IConsumer<EntityDeletedEvent<CommissionRule>>
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public CommissionRuleEventConsumer(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager;
        }

        public async Task HandleEventAsync(EntityInsertedEvent<CommissionRule> eventMessage)
        {
            await _staticCacheManager.RemoveByPrefixAsync("marketplace.commission.rules");
        }

        public async Task HandleEventAsync(EntityUpdatedEvent<CommissionRule> eventMessage)
        {
            await _staticCacheManager.RemoveByPrefixAsync("marketplace.commission.rules");
        }

        public async Task HandleEventAsync(EntityDeletedEvent<CommissionRule> eventMessage)
        {
            await _staticCacheManager.RemoveByPrefixAsync("marketplace.commission.rules");
        }
    }
}
