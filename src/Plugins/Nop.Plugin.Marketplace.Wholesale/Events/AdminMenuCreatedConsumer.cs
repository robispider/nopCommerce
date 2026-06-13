using System.Linq;
using System.Threading.Tasks;
using Nop.Services.Events;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Marketplace.Wholesale.Events
{
    /// <summary>
    /// Injects "Wholesale Catalog" into the Admin Sidebar.
    /// </summary>
    public class AdminMenuCreatedConsumer : IConsumer<AdminMenuCreatedEvent>
    {
        public Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
        {
            // Create the Wholesale Catalog menu item
            var wholesaleNode = new AdminMenuItem
            {
                SystemName = "Marketplace.WholesaleCatalog",
                Title = "Wholesale Catalog",
                Url = "/Admin/SupplierWholesale/List", // Exact same URL format as Phase 1 / Storefront
                IconClass = "far fa-dot-circle",
                Visible = true
            };
            var sourcingNode = new AdminMenuItem
            {
                SystemName = "Marketplace.B2BSourcing",
                Title = "B2B Sourcing Portal",
                Url = "/Admin/B2BSourcing/List",
                IconClass = "fas fa-shopping-cart",
                Visible = true
            };
            var sourcedCatalogNode = new AdminMenuItem
            {
                SystemName = "Marketplace.MySourcedCatalog",
                Title = "My Sourced Catalog",
                Url = "/Admin/MySourcedCatalog/List",
                IconClass = "fas fa-box-open",
                Visible = true
            };

            // Try to find the "Marketplace" parent menu (using both possible SystemNames from Phase 1)
            var marketplaceNode = eventMessage.RootMenuItem.ChildNodes
                .FirstOrDefault(x => x.SystemName == "Marketplace.Admin" || x.SystemName == "Marketplace");

            if (marketplaceNode != null)
            {
                // If it finds the Marketplace menu, put "Wholesale Catalog" inside it
                marketplaceNode.ChildNodes.Add(wholesaleNode);
                marketplaceNode.ChildNodes.Add(sourcingNode); // <-- ADD THIS LINE
                marketplaceNode.ChildNodes.Add(sourcedCatalogNode);
            }
            else
            {
                // Fallback: If it doesn't find it, put it right below the Dashboard
                eventMessage.RootMenuItem.ChildNodes.Insert(1, wholesaleNode);
                eventMessage.RootMenuItem.ChildNodes.Insert(2, sourcingNode); // <-- ADD THIS LINE
                eventMessage.RootMenuItem.ChildNodes.Insert(3, sourcedCatalogNode);
            }

            return Task.CompletedTask;
        }
    }
}