using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Marketplace.Business.Services;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Marketplace.Business.Controllers
{
    [AuthorizeAdmin] // Require admin/vendor access
    [AutoValidateAntiforgeryToken]
    public class MarketplaceCatalogController : BasePluginController
    {
        private readonly IMarketplaceCatalogService _catalogService;

        public MarketplaceCatalogController(IMarketplaceCatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        [HttpPost]
        public async Task<IActionResult> ImportSupplierProduct(int supplierProductId, decimal retailMarginPercentage)
        {
            try
            {
                await _catalogService.ImportSupplierProductAsync(supplierProductId, retailMarginPercentage);

                // Return a success JSON response that can be handled by AJAX in the UI
                return Json(new { success = true, message = "Product successfully cloned to your catalog." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}