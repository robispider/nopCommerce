using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Marketplace.Wholesale.Models;
using Nop.Plugin.Marketplace.Wholesale.Services;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Media;
using Nop.Services.Vendors;
using Nop.Services.Security; // <--- ADDED for Permissions
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Marketplace.Wholesale.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [AutoValidateAntiforgeryToken]
    public class B2BSourcingController : BasePluginController
    {
        private readonly ISupplierProductService _supplierProductService;
        private readonly IProductService _productService;
        private readonly IVendorService _vendorService;
        private readonly IPictureService _pictureService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IWorkContext _workContext;
        private readonly IPermissionService _permissionService; // <--- ADDED

        public B2BSourcingController(
            ISupplierProductService supplierProductService,
            IProductService productService,
            IVendorService vendorService,
            IPictureService pictureService,
            IPriceFormatter priceFormatter,
            IWorkContext workContext,
            IPermissionService permissionService)
        {
            _supplierProductService = supplierProductService;
            _productService = productService;
            _vendorService = vendorService;
            _pictureService = pictureService;
            _priceFormatter = priceFormatter;
            _workContext = workContext;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> List()
        {
            // SECURE: Only Resellers can access the sourcing portal
            if (!await _permissionService.AuthorizeAsync("AccessResellerPortal"))
                return AccessDeniedView();

            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return AccessDeniedView();

            var model = new B2BProductSearchModel();
            model.SetGridPageSize();

            return View("~/Plugins/Marketplace.Wholesale/Views/B2BSourcing/List.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> List(B2BProductSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync("AccessResellerPortal"))
                return Json(new B2BProductListModel());

            var currentVendor = await _workContext.GetCurrentVendorAsync();
            if (currentVendor == null)
                return Json(new B2BProductListModel());

            // 1. Fetch B2B records NOT owned by this reseller
            var b2bProducts = await _supplierProductService.SearchB2BProductsAsync(
                excludeVendorId: currentVendor.Id, // Hides their own products
                pageIndex: searchModel.Page - 1,
                pageSize: searchModel.PageSize);

            // 2. Prepare the grid models using Elite Async Enumerable
            var model = await new B2BProductListModel().PrepareToGridAsync(searchModel, b2bProducts, () =>
            {
                async IAsyncEnumerable<B2BProductModel> PrepareB2BModelsAsync()
                {
                    foreach (var sp in b2bProducts)
                    {
                        var product = await _productService.GetProductByIdAsync(sp.ProductId);

                        // SAFETY: Do not show deleted or unpublished products!
                        if (product == null || product.Deleted || !product.Published)
                            continue;

                        var supplier = await _vendorService.GetVendorByIdAsync(sp.VendorId);
                        var pictures = await _pictureService.GetPicturesByProductIdAsync(product.Id, 1);

                        var picUrl = pictures.FirstOrDefault() != null
                            ? (await _pictureService.GetPictureUrlAsync(pictures.FirstOrDefault(), 75)).Url
                            : await _pictureService.GetDefaultPictureUrlAsync(75);

                        // Build UI Badges
                        string badges = "";
                        if (sp.IsDropshipEnabled)
                            badges += "<span class='badge bg-success'>Dropship</span> ";
                        if (sp.IsPreorderEnabled)
                            badges += "<span class='badge bg-warning'>Preorder</span>";

                        var formattedPrice = await _priceFormatter.FormatPriceAsync(sp.WholesalePrice);

                        yield return new B2BProductModel
                        {
                            Id = sp.Id,
                            ProductId = sp.ProductId,
                            PictureThumbnailUrl = picUrl,
                            ProductName = product.Name,
                            SupplierName = supplier?.Name ?? "Unknown Supplier",
                            WholesalePrice = formattedPrice,
                            MinimumOrderQuantity = sp.MinimumOrderQuantity,
                            BadgesHtml = badges
                        };
                    }
                }

                return PrepareB2BModelsAsync();
            });

            return Json(model);
        }
    }
}