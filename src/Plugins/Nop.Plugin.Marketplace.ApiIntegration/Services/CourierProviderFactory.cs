using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Marketplace.ApiIntegration.Services
{
    public interface ICourierProviderFactory
    {
        ICourierProvider GetProvider(string systemName);
    }

    public class CourierProviderFactory : ICourierProviderFactory
    {
        private readonly IEnumerable<ICourierProvider> _providers;

        public CourierProviderFactory(IEnumerable<ICourierProvider> providers)
        {
            _providers = providers;
        }

        public ICourierProvider GetProvider(string systemName)
        {
            return _providers.FirstOrDefault(p => p.SystemName.ToLower() == systemName.ToLower());
        }
    }
}