using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Marketplace.Business.Components
{
    public class MarketplaceAccountMenuViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Marketplace.Business/Views/AccountMenu/Default.cshtml");
        }
    }
}