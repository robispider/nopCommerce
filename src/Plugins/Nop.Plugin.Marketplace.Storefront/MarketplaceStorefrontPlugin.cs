using Nop.Plugin.Marketplace.Storefront.Components;
using Nop.Services.Cms;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Marketplace.Storefront
{
    /// <summary>
    /// Handles installation, uninstallation, and widget zone mappings for the Storefront Engine.
    /// </summary>
    public class MarketplaceStorefrontPlugin : BasePlugin, IWidgetPlugin
    {
        public MarketplaceStorefrontPlugin()
        {
        }

        public override async Task InstallAsync()
        {
            // In Step 2, we will add database table migration triggers here if needed,
            // though FluentMigrator handles [NopMigration] automatically.

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            // Cleanup logic for configuration settings will go here.
            await base.UninstallAsync();
        }

     

        #region IWidgetPlugin Implementation

        public bool HideInWidgetList => false;

        public Task<IList<string>> GetWidgetZonesAsync()
        {
            return Task.FromResult<IList<string>>(new List<string>
            {
                PublicWidgetZones.HeadHtmlTag
            });
        }

        public Type GetWidgetViewComponent(string widgetZone)
        {
            // Map the HeadHtmlTag zone to our new ViewComponent
            if (widgetZone == PublicWidgetZones.HeadHtmlTag)
            {
                return typeof(StorefrontBrandingViewComponent);
            }

            return null;
        }

        #endregion
    }
}