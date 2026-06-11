using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Marketplace.Storefront.Domains;
using Nop.Plugin.Marketplace.Storefront.Models;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Marketplace.Storefront.Controllers
{
    [AuthorizeAdmin] // Requires backend access
    [Area(AreaNames.ADMIN)] // Routes via the Admin area
    [AutoValidateAntiforgeryToken]
    public class StorefrontAdminController : BasePluginController
    {
        private readonly IWorkContext _workContext;
        private readonly IRepository<ResellerStorefront> _storefrontRepository;
        private readonly INotificationService _notificationService;

        public StorefrontAdminController(
            IWorkContext workContext,
            IRepository<ResellerStorefront> storefrontRepository,
            INotificationService notificationService)
        {
            _workContext = workContext;
            _storefrontRepository = storefrontRepository;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Configure()
        {
            // 1. Ensure the logged-in user is actually a Reseller (Vendor)
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
            {
                _notificationService.ErrorNotification("Only verified Resellers can configure a storefront.");
                return RedirectToAction("Index", "Home", new { area = AreaNames.ADMIN });
            }

            // 2. Fetch their existing storefront
            var storefront = await _storefrontRepository.Table
                .FirstOrDefaultAsync(x => x.VendorId == vendor.Id);

            var model = new StorefrontConfigurationModel();
            if (storefront != null)
            {
                model.Id = storefront.Id;
                model.StoreName = storefront.StoreName;
                model.UrlSlug = storefront.UrlSlug;
                model.PrimaryColorHex = storefront.PrimaryColorHex;
                model.LogoPictureId = storefront.LogoPictureId;
                model.BannerPictureId = storefront.BannerPictureId;
                model.IsActive = storefront.IsActive;
            }

            return View("~/Plugins/Marketplace.Storefront/Views/StorefrontAdmin/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(StorefrontConfigurationModel model)
        {
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return View("~/Plugins/Marketplace.Storefront/Views/StorefrontAdmin/Configure.cshtml", model);

            // Clean up the URL slug
            var slug = model.UrlSlug?.ToLowerInvariant().Replace(" ", "-").Trim();

            var storefront = await _storefrontRepository.Table
                .FirstOrDefaultAsync(x => x.VendorId == vendor.Id);

            if (storefront == null)
            {
                // Insert New
                storefront = new ResellerStorefront
                {
                    VendorId = vendor.Id,
                    StoreName = model.StoreName,
                    UrlSlug = slug,
                    PrimaryColorHex = model.PrimaryColorHex,
                    LogoPictureId = model.LogoPictureId,
                    BannerPictureId = model.BannerPictureId,
                    IsActive = model.IsActive
                };
                await _storefrontRepository.InsertAsync(storefront);
            }
            else
            {
                // Update Existing
                storefront.StoreName = model.StoreName;
                storefront.UrlSlug = slug;
                storefront.PrimaryColorHex = model.PrimaryColorHex;
                storefront.LogoPictureId = model.LogoPictureId;
                storefront.BannerPictureId = model.BannerPictureId;
                storefront.IsActive = model.IsActive;
                await _storefrontRepository.UpdateAsync(storefront);
            }

            // The moment we hit Insert/Update, our 'StorefrontCacheEventConsumer' from Step 3 
            // automatically triggers via EF Core and wipes the Redis cache. The changes are instantly live!

            _notificationService.SuccessNotification("Your Storefront branding has been updated successfully.");

            return RedirectToAction("Configure");
        }
    }
}