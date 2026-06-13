using Nop.Services.Events;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Marketplace.Storefront.Events
{
    /// <summary>
    /// Injects "My Storefront" into the Admin Sidebar.
    /// </summary>
    public class AdminMenuEventConsumer : IConsumer<AdminMenuCreatedEvent>
    {
        public Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
        {
            var pluginNode = new AdminMenuItem
            {
                SystemName = "Marketplace.Storefront.Manage",
                Title = "My Storefront",
                Url = "/Admin/StorefrontAdmin/Configure", // Exact same URL format as Phase 1
                IconClass = "fas fa-store-alt", // A slightly different store icon
                Visible = true
            };

            // Let's try to find the "Marketplace" menu you created in Phase 1
            var marketplaceNode = eventMessage.RootMenuItem.ChildNodes
                .FirstOrDefault(x => x.SystemName == "Marketplace.Admin");

            if (marketplaceNode != null)
            {
                // If it finds the Marketplace menu, put "My Storefront" inside it!
                marketplaceNode.ChildNodes.Add(pluginNode);
            }
            else
            {
                // Fallback: If it doesn't find it, put it right below the Dashboard (Index 1) just like Phase 1
                eventMessage.RootMenuItem.ChildNodes.Insert(1, pluginNode);
            }

            return Task.CompletedTask;
        }
    }
}