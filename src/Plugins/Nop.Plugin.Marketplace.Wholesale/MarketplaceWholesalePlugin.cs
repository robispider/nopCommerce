using Nop.Services.Plugins;

namespace Nop.Plugin.Marketplace.Wholesale
{
    public class MarketplaceWholesalePlugin : BasePlugin
    {
        public override async Task InstallAsync()
        {
            // Migrations handle table creation automatically in v4.90!
            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await base.UninstallAsync();
        }
    }
}