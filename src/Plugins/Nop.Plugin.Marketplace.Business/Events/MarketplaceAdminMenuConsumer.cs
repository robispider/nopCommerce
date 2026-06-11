using Nop.Services.Events;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Marketplace.Business.Events
{
    /// <summary>
    /// Intercepts the native admin menu creation event to inject the Marketplace dashboard.
    /// This replaces the deprecated IAdminMenuPlugin.
    /// </summary>
    public class MarketplaceAdminMenuConsumer : IConsumer<AdminMenuCreatedEvent>
    {
        public Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
        {
            var marketplaceNode = new AdminMenuItem
            {
                SystemName = "Marketplace.Admin",
                Title = "Marketplace",
                IconClass = "fas fa-store", // Uses FontAwesome
                Visible = true
            };

            marketplaceNode.ChildNodes.Add(new AdminMenuItem
            {
                SystemName = "Marketplace.Admin.KYC",
                Title = "Pending KYC Approvals",
                Url = "/Admin/MarketplaceKyc/List",
                IconClass = "far fa-id-card",
                Visible = true
            });

            marketplaceNode.ChildNodes.Add(new AdminMenuItem
            {
                SystemName = "Marketplace.Admin.Settings",
                Title = "Marketplace Settings",
                Url = "/Admin/MarketplaceSettings/Configure",
                IconClass = "fas fa-cogs",
                Visible = true
            });

            // Insert our menu near the top (index 1 is usually right below the Dashboard)
            eventMessage.RootMenuItem.ChildNodes.Insert(1, marketplaceNode);

            return Task.CompletedTask;
        }
    }
}