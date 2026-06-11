using Nop.Services.Localization;
using Nop.Services.Plugins;

namespace Nop.Plugin.Marketplace.Core
{
    public class MarketplaceCorePlugin : BasePlugin
    {
        private readonly ILocalizationService _localizationService;

        public MarketplaceCorePlugin(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public override async Task InstallAsync()
        {
            // Install localization keys (useful for multi-language admin panels)
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Marketplace.Core.Admin.BusinessName"] = "Business/Legal Name",
                ["Plugins.Marketplace.Core.Admin.TaxId"] = "Tax Identification Number"
            });

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            // Clean up localization keys
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Marketplace.Core");

            await base.UninstallAsync();
        }
    }
}