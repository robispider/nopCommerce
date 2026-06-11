using Nop.Services.Events;
using Nop.Services.Security;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Marketplace.Storefront.Events
{
    /// <summary>
    /// Injects a "My Storefront" menu item into the nopCommerce Admin Sidebar.
    /// </summary>
    public class AdminMenuEventConsumer : IConsumer<AdminMenuCreatedEvent>
    {
        private readonly IPermissionService _permissionService;

        public AdminMenuEventConsumer(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public async Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
        {
            // Bypass the missing StandardPermissionProvider by using the system name string directly.
            // If they don't have access to the admin panel at all, do nothing.
            if (!await _permissionService.AuthorizeAsync("AccessAdminPanel"))
                return;

            var pluginNode = new AdminMenuItem
            {
                SystemName = "Marketplace.Storefront.Manage",
                Title = "My Storefront",

                // By providing a full URL string, the AdminMenuItem property setter will 
                // automatically parse ControllerName ("StorefrontAdmin") and ActionName ("Configure") for us!
                Url = "~/Admin/StorefrontAdmin/Configure",

                IconClass = "fas fa-store", // Uses FontAwesome icon natively included in nopCommerce
                Visible = true
            };

            // In recent versions, the menu tree is held in the 'RootMenuItem' property
            eventMessage.RootMenuItem.ChildNodes.Add(pluginNode);
        }
    }
}