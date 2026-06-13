using System.Threading.Tasks;
using Nop.Services.Plugins;

namespace Nop.Plugin.Marketplace.Accounting
{
    public class MarketplaceAccountingPlugin : BasePlugin
    {
        public override async Task InstallAsync()
        {
            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await base.UninstallAsync();
        }
    }
}