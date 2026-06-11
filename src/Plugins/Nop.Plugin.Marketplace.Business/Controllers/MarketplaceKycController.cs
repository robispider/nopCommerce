using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Marketplace.Business.Controllers
{
    [AuthorizeAdmin] // Secures the page to Admins only
    [Area(AreaNames.ADMIN)] // Tells the routing engine this belongs in the Admin panel
    [AutoValidateAntiforgeryToken]
    [Route("Admin/MarketplaceKyc/[action]")] // Forces the exact URL from our menu
    public class MarketplaceKycController : BasePluginController
    {
        public IActionResult List()
        {
            // Point exactly to our custom plugin view
            return View("~/Plugins/Marketplace.Business/Views/MarketplaceKyc/List.cshtml");
        }
    }
}