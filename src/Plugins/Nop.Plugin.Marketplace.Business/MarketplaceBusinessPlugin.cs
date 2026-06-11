using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Plugin.Marketplace.Business.Settings;
using Nop.Services.Cms; // <--- ADDED: This is where IWidgetPlugin lives now
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Menu; // <--- Provides IAdminMenuPlugin and AdminMenuItem
using Nop.Web.Framework.Infrastructure; // <--- Provides AdminWidgetZones and PublicWidgetZones
using Nop.Plugin.Marketplace.Business.Components;

namespace Nop.Plugin.Marketplace.Business
{
    public class MarketplaceBusinessPlugin : BasePlugin, IWidgetPlugin
    {
        private readonly ICustomerService _customerService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;

        public MarketplaceBusinessPlugin(
            ICustomerService customerService,
            IPermissionService permissionService,
            ISettingService settingService)
        {
            _customerService = customerService;
            _permissionService = permissionService;
            _settingService = settingService;
        }

        // --- PLUGIN INSTALLATION LOGIC ---
        public override async Task InstallAsync()
        {
            await _settingService.SaveSettingAsync(new MarketplaceBusinessSettings());
            await SeedCustomerRoleAsync("Marketplace Supplier", "MarketplaceSupplier");
            await SeedCustomerRoleAsync("Marketplace Reseller", "MarketplaceReseller");
            await SeedPermissionAsync("Access Supplier Portal", "AccessSupplierPortal", "MarketplaceSupplier");
            await SeedPermissionAsync("Access Reseller Portal", "AccessResellerPortal", "MarketplaceReseller");

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _settingService.DeleteSettingAsync<MarketplaceBusinessSettings>();
            await base.UninstallAsync();
        }

        private async Task SeedCustomerRoleAsync(string name, string systemName)
        {
            var role = await _customerService.GetCustomerRoleBySystemNameAsync(systemName);
            if (role == null)
            {
                role = new CustomerRole
                {
                    Name = name,
                    SystemName = systemName,
                    Active = true,
                    EnablePasswordLifetime = false
                };
                await _customerService.InsertCustomerRoleAsync(role);
            }
        }

        private async Task SeedPermissionAsync(string name, string systemName, string roleSystemName)
        {
            var permission = (await _permissionService.GetAllPermissionRecordsAsync())
                                .FirstOrDefault(p => p.SystemName == systemName);

            if (permission == null)
            {
                permission = new PermissionRecord
                {
                    Name = name,
                    SystemName = systemName,
                    Category = "Marketplace"
                };
                await _permissionService.InsertPermissionRecordAsync(permission);
            }

            var role = await _customerService.GetCustomerRoleBySystemNameAsync(roleSystemName);
            if (role != null)
            {
                var mappingExists = (await _permissionService.GetMappingByPermissionRecordIdAsync(permission.Id))
                                        .Any(m => m.CustomerRoleId == role.Id);
                if (!mappingExists)
                {
                    await _permissionService.InsertPermissionRecordCustomerRoleMappingAsync(new PermissionRecordCustomerRoleMapping
                    {
                        PermissionRecordId = permission.Id,
                        CustomerRoleId = role.Id
                    });
                }
            }
        }

        // --- ADMIN MENU INJECTION ---
        // NOTE: Uses AdminMenuItem instead of SiteMapNode
        //public Task ManageSiteMapAsync(AdminMenuItem rootNode)
        //{
        //    var marketplaceNode = new AdminMenuItem
        //    {
        //        SystemName = "Marketplace.Admin",
        //        Title = "Marketplace",
        //        IconClass = "fas fa-store", // Uses FontAwesome
        //        Visible = true
        //    };

        //    marketplaceNode.ChildNodes.Add(new AdminMenuItem
        //    {
        //        SystemName = "Marketplace.Admin.KYC",
        //        Title = "Pending KYC Approvals",
        //        Url = "/Admin/MarketplaceKyc/List",
        //        IconClass = "far fa-id-card",
        //        Visible = true
        //    });

        //    marketplaceNode.ChildNodes.Add(new AdminMenuItem
        //    {
        //        SystemName = "Marketplace.Admin.Settings",
        //        Title = "Marketplace Settings",
        //        Url = "/Admin/MarketplaceSettings/Configure",
        //        IconClass = "fas fa-cogs",
        //        Visible = true
        //    });

        //    // Insert our menu near the top (e.g., right below the Dashboard)
        //    rootNode.ChildNodes.Insert(1, marketplaceNode);

        //    return Task.CompletedTask;
        //}

        // --- WIDGET INJECTION ---
        public bool HideInWidgetList => false;

        public Type GetWidgetViewComponent(string widgetZone)
        {
            // Return our custom ViewComponent based on the injected zone
            if (widgetZone == AdminWidgetZones.DashboardTop)
                return typeof(MarketplaceAdminDashboardViewComponent);

            return typeof(MarketplaceAccountMenuViewComponent);
        }

        public Task<IList<string>> GetWidgetZonesAsync()
        {
            return Task.FromResult<IList<string>>(new List<string>
            {
                AdminWidgetZones.DashboardTop,             // Injects into the Admin Dashboard
                PublicWidgetZones.AccountNavigationAfter   // Injects into the Public "My Account" menu
            });
        }
    }
}