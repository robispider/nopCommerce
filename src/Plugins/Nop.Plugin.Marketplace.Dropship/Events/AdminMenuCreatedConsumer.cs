using System.Linq;
using System.Threading.Tasks;
using Nop.Services.Events;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Marketplace.Dropship.Events
{
    public class AdminMenuCreatedConsumer : IConsumer<AdminMenuCreatedEvent>
    {
        public Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
        {
            var dropshipNode = new AdminMenuItem
            {
                SystemName = "Marketplace.DropshipOrders",
                Title = "Dropship Orders",
                Url = "/Admin/SupplierDropship/List",
                IconClass = "fas fa-shipping-fast",
                Visible = true
            };

            var marketplaceNode = eventMessage.RootMenuItem.ChildNodes
                .FirstOrDefault(x => x.SystemName == "Marketplace.Admin" || x.SystemName == "Marketplace");

            if (marketplaceNode != null)
            {
                marketplaceNode.ChildNodes.Add(dropshipNode);
            }
            else
            {
                eventMessage.RootMenuItem.ChildNodes.Insert(2, dropshipNode);
            }

            return Task.CompletedTask;
        }
    }
}