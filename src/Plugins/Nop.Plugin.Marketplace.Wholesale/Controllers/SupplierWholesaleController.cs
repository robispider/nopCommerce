using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Marketplace.Wholesale.Domains;
using Nop.Plugin.Marketplace.Wholesale.Models;
using Nop.Plugin.Marketplace.Wholesale.Services;
using Nop.Services.Catalog;
using Nop.Services.Security; // <--- REQUIRED FOR ACCESS CONTROL
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Marketplace.Wholesale.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [AutoValidateAntiforgeryToken]
    public class SupplierWholesaleController : BasePluginController
    {
        private readonly IProductService _productService;
        private readonly ISupplierProductService _supplierProductService;
        private readonly IWorkContext _workContext;
        private readonly IPermissionService _permissionService;

        public SupplierWholesaleController(
            IProductService productService,
            ISupplierProductService supplierProductService,
            IWorkContext workContext,
            IPermissionService permissionService)
        {
            _productService = productService;
            _supplierProductService = supplierProductService;
            _workContext = workContext;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> List()
        {
            // ELITE SECURITY: Block anyone who does not have the Supplier Role/Permission
            if (!await _permissionService.AuthorizeAsync("AccessSupplierPortal"))
                return AccessDeniedView();

            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return AccessDeniedView();

            var model = new SupplierProductSearchModel();
            model.SetGridPageSize();

            return View("~/Plugins/Marketplace.Wholesale/Views/SupplierWholesale/List.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> List(SupplierProductSearchModel searchModel)
        {
            // SECURE AJAX GRID: Return empty grid if unauthorized (prevents DataTable crash)
            if (!await _permissionService.AuthorizeAsync("AccessSupplierPortal"))
                return Json(new SupplierProductListModel());

            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return Json(new SupplierProductListModel());

            var vendorId = vendor.Id;

            var products = await _productService.SearchProductsAsync(
                vendorId: vendorId,
                showHidden: true,
                pageIndex: searchModel.Page - 1,
                pageSize: searchModel.PageSize
            );

            var model = new SupplierProductListModel().PrepareToGrid(searchModel, products, () =>
            {
                return products.Select(product =>
                {
                    var supplierProduct = _supplierProductService.GetByProductIdAsync(product.Id).Result;

                    return new SupplierProductModel
                    {
                        Id = product.Id,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        WholesalePrice = supplierProduct?.WholesalePrice ?? 0,
                        MinimumOrderQuantity = supplierProduct?.MinimumOrderQuantity ?? 0,
                        IsDropshipEnabled = supplierProduct?.IsDropshipEnabled ?? false,
                        IsPreorderEnabled = supplierProduct?.IsPreorderEnabled ?? false,
                        LeadTimeDays = supplierProduct?.LeadTimeDays ?? 0
                    };
                });
            });

            return Json(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            // SECURE EDIT PAGE
            if (!await _permissionService.AuthorizeAsync("AccessSupplierPortal"))
                return AccessDeniedView();

            var vendor = await _workContext.GetCurrentVendorAsync();
            var product = await _productService.GetProductByIdAsync(id);

            if (product == null || vendor == null || product.VendorId != vendor.Id)
                return RedirectToAction("List");

            var supplierProduct = await _supplierProductService.GetByProductIdAsync(id);

            var model = new SupplierProductModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                WholesalePrice = supplierProduct?.WholesalePrice ?? 0,
                MinimumOrderQuantity = supplierProduct?.MinimumOrderQuantity ?? 0,
                IsDropshipEnabled = supplierProduct?.IsDropshipEnabled ?? false,
                IsPreorderEnabled = supplierProduct?.IsPreorderEnabled ?? false,
                LeadTimeDays = supplierProduct?.LeadTimeDays ?? 0
            };

            return View("~/Plugins/Marketplace.Wholesale/Views/SupplierWholesale/Edit.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SupplierProductModel model)
        {
            // SECURE POST SUBMISSION
            if (!await _permissionService.AuthorizeAsync("AccessSupplierPortal"))
                return AccessDeniedView();

            var vendor = await _workContext.GetCurrentVendorAsync();
            var product = await _productService.GetProductByIdAsync(model.ProductId);

            if (product == null || vendor == null || product.VendorId != vendor.Id)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                var supplierProduct = await _supplierProductService.GetByProductIdAsync(model.ProductId);
                if (supplierProduct == null)
                {
                    supplierProduct = new SupplierProduct
                    {
                        ProductId = model.ProductId,
                        VendorId = product.VendorId,
                        WholesalePrice = model.WholesalePrice,
                        MinimumOrderQuantity = model.MinimumOrderQuantity,
                        IsDropshipEnabled = model.IsDropshipEnabled,
                        IsPreorderEnabled = model.IsPreorderEnabled,
                        LeadTimeDays = model.LeadTimeDays
                    };
                    await _supplierProductService.InsertSupplierProductAsync(supplierProduct);
                }
                else
                {
                    supplierProduct.WholesalePrice = model.WholesalePrice;
                    supplierProduct.MinimumOrderQuantity = model.MinimumOrderQuantity;
                    supplierProduct.IsDropshipEnabled = model.IsDropshipEnabled;
                    supplierProduct.IsPreorderEnabled = model.IsPreorderEnabled;
                    supplierProduct.LeadTimeDays = model.LeadTimeDays;
                    await _supplierProductService.UpdateSupplierProductAsync(supplierProduct);
                }

                return RedirectToAction("List");
            }

            return View("~/Plugins/Marketplace.Wholesale/Views/SupplierWholesale/Edit.cshtml", model);
        }
    }
}