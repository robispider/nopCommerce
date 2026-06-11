using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Marketplace.Storefront.Models;
using Nop.Plugin.Marketplace.Storefront.Services;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Marketplace.Storefront.Controllers
{
    public class StorefrontController : BasePluginController
    {
        private readonly IStorefrontContext _storefrontContext;
        private readonly IProductService _productService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IPictureService _pictureService;

        public StorefrontController(
            IStorefrontContext storefrontContext,
            IProductService productService,
            IProductModelFactory productModelFactory,
            IPictureService pictureService)
        {
            _storefrontContext = storefrontContext;
            _productService = productService;
            _productModelFactory = productModelFactory;
            _pictureService = pictureService;
        }

        public async Task<IActionResult> Index(string slug)
        {
            // 1. Get the active storefront using the Tier-1 Cached Context
            var storefront = await _storefrontContext.GetCurrentStorefrontAsync();

            if (storefront == null || !storefront.IsActive)
                return NotFound(); // Return standard 404 if slug is invalid or store is offline

            // 2. Resolve Banner Image if uploaded
            string bannerUrl = null;
            if (storefront.BannerPictureId > 0)
            {
                bannerUrl = await _pictureService.GetPictureUrlAsync(storefront.BannerPictureId);
            }

            // 3. Fetch products OWNED by this Reseller (VendorId)
            // Because of Phase 1 "Light Cloning", these are native nopCommerce products!
            var products = await _productService.SearchProductsAsync(
                vendorId: storefront.VendorId,
                visibleIndividuallyOnly: true
            );

            // 4. Transform native Product entities into standard UI Models 
            // (This automatically handles pricing, discounts, and main product images)
            var productModels = (await _productModelFactory.PrepareProductOverviewModelsAsync(products)).ToList();

            // 5. Construct the View Model
            var model = new StorefrontIndexModel
            {
                StoreName = storefront.StoreName,
                BannerUrl = bannerUrl,
                Products = productModels
            };

            return View("~/Plugins/Marketplace.Storefront/Views/Storefront/Index.cshtml", model);
        }
    }
}