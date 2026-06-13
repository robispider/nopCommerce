using System.Threading.Tasks;
using Nop.Services.Plugins;

namespace Nop.Plugin.Marketplace.Wallet
{
    public class MarketplaceWalletPlugin : BasePlugin
    {
        public override async Task InstallAsync()
        {
            // Wallet relies entirely on Event Consumers, no scheduled tasks needed.
            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await base.UninstallAsync();
        }
    }
}