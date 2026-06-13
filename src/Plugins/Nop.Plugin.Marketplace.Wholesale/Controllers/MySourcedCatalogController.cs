using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Wholesale.Models;
using Nop.Plugin.Marketplace.Wholesale.Services;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Media;
using Nop.Services.Vendors;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Marketplace.Wholesale.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [AutoValidateAntiforgeryToken]
    public class MySourcedCatalogController : BasePluginController
    {
        private readonly IWorkContext _workContext;
        private readonly IRepository<ResellerProductMapping> _mappingRepository;
        private readonly IProductService _productService;
        private readonly IVendorService _vendorService;
        private readonly IPictureService _pictureService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ISupplierProductService _supplierProductService;

        public MySourcedCatalogController(
            IWorkContext workContext,
            IRepository<ResellerProductMapping> mappingRepository,
            IProductService productService,
            IVendorService vendorService,
            IPictureService pictureService,
            IPriceFormatter priceFormatter,
            ISupplierProductService supplierProductService)
        {
            _workContext = workContext;
            _mappingRepository = mappingRepository;
            _productService = productService;
            _vendorService = vendorService;
            _pictureService = pictureService;
            _priceFormatter = priceFormatter;
            _supplierProductService = supplierProductService;
        }

        public async Task<IActionResult> List()
        {
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return AccessDeniedView();

            var model = new SourcedProductSearchModel();
            model.SetGridPageSize();

            return View("~/Plugins/Marketplace.Wholesale/Views/MySourcedCatalog/List.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> List(SourcedProductSearchModel searchModel)
        {
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return Json(new SourcedProductListModel());

            // 1. Fetch only mappings owned by this Reseller
            var query = _mappingRepository.Table
                .Where(x => x.ResellerBusinessId == vendor.Id)
                .OrderByDescending(x => x.CreatedOnUtc);

            var mappingsList = query.Skip((searchModel.Page - 1) * searchModel.PageSize)
                                    .Take(searchModel.PageSize)
                                    .ToList();

            var pagedMappings = mappingsList.ToPagedList(searchModel);

            // 2. Prepare the grid models using true IAsyncEnumerable (Elite Async Fix)
            var model = await new SourcedProductListModel().PrepareToGridAsync(searchModel, pagedMappings, () =>
            {
                // This is the native nopCommerce way to yield async grid results
                async IAsyncEnumerable<SourcedProductModel> PrepareMappingsAsync()
                {
                    foreach (var map in pagedMappings)
                    {
                        var retailProduct = await _productService.GetProductByIdAsync(map.ResellerCoreProductId);
                        var supplierProduct = await _productService.GetProductByIdAsync(map.SupplierCoreProductId);
                        var supplier = supplierProduct != null ? await _vendorService.GetVendorByIdAsync(supplierProduct.VendorId) : null;
                        var supplierB2BSettings = await _supplierProductService.GetByProductIdAsync(map.SupplierCoreProductId);

                        // Picture
                        var pictures = await _pictureService.GetPicturesByProductIdAsync(retailProduct?.Id ?? 0, 1);
                        var picUrl = pictures.FirstOrDefault() != null
                            ? (await _pictureService.GetPictureUrlAsync(pictures.FirstOrDefault(), 75)).Url
                            : await _pictureService.GetDefaultPictureUrlAsync(75);

                        // Badges
                        string policyBadge = map.SelectedProcurementPolicyId switch
                        {
                            (int)ProcurementSettlementPolicy.FullEscrow => "<span class='badge bg-info'>Full Escrow (Zero Deposit)</span>",
                            (int)ProcurementSettlementPolicy.ResellerPrepay => "<span class='badge bg-danger'>Prepay Required</span>",
                            (int)ProcurementSettlementPolicy.RollingReserve => "<span class='badge bg-warning text-dark'>Rolling Reserve</span>",
                            (int)ProcurementSettlementPolicy.CreditLimit => "<span class='badge bg-success'>Credit Limit</span>",
                            _ => "<span class='badge bg-secondary'>Unknown</span>"
                        };

                        string marginBadge = $"<span class='badge bg-success'>+{map.MarginPercentage:F2}%</span>";

                        // Yielding the result prevents memory bloat by streaming the models one by one
                        yield return new SourcedProductModel
                        {
                            Id = map.Id,
                            PictureThumbnailUrl = picUrl,
                            RetailProductName = retailProduct?.Name ?? "Deleted Product",
                            SupplierName = supplier?.Name ?? "Unknown Supplier",
                            WholesaleCost = await _priceFormatter.FormatPriceAsync(supplierB2BSettings?.WholesalePrice ?? 0),
                            RetailPrice = await _priceFormatter.FormatPriceAsync(retailProduct?.Price ?? 0),
                            MarginHtml = marginBadge,
                            ProcurementPolicyHtml = policyBadge
                        };
                    }
                }

                return PrepareMappingsAsync();
            });

            return Json(model);
        }
    }
}