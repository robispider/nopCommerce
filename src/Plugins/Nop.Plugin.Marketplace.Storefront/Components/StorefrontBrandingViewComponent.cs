using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Marketplace.Storefront.Models;
using Nop.Plugin.Marketplace.Storefront.Services;
using Nop.Services.Media;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Marketplace.Storefront.Components
{
    /// <summary>
    /// Injects dynamic CSS variables into the <head> of the document 
    /// if the user is currently browsing a Reseller Storefront.
    /// </summary>
    public class StorefrontBrandingViewComponent : NopViewComponent
    {
        private readonly IStorefrontContext _storefrontContext;
        private readonly IPictureService _pictureService;

        public StorefrontBrandingViewComponent(
            IStorefrontContext storefrontContext,
            IPictureService pictureService)
        {
            _storefrontContext = storefrontContext;
            _pictureService = pictureService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var storefront = await _storefrontContext.GetCurrentStorefrontAsync();

            // If we are not on a storefront route, render nothing (keep master theme).
            if (storefront == null || !storefront.IsActive)
                return Content(string.Empty);

            // Resolve the logo URL if the Reseller uploaded one.
            string logoUrl = null;
            if (storefront.LogoPictureId > 0)
            {
                logoUrl = await _pictureService.GetPictureUrlAsync(storefront.LogoPictureId);
            }

            var model = new StorefrontBrandingModel
            {
                StoreName = storefront.StoreName,
                PrimaryColorHex = !string.IsNullOrWhiteSpace(storefront.PrimaryColorHex) ? storefront.PrimaryColorHex : "#000000", // Fallback
                LogoUrl = logoUrl
            };

            return View("~/Plugins/Marketplace.Storefront/Views/StorefrontBranding/Default.cshtml", model);
        }
    }
}