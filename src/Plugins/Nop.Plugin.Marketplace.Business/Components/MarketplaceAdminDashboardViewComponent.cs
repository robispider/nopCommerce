using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Marketplace.Business.Components
{
    public class MarketplaceAdminDashboardViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            // Path must point exactly to where the file will be compiled in the Web layer
            return View("~/Plugins/Marketplace.Business/Views/AdminDashboard/Default.cshtml");
        }
    }
}