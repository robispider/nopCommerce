using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents; // <--- Add this namespace
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Marketplace.Business.Components
{
    public class MarketplaceContextBadgeViewComponent : NopViewComponent
    {
        private readonly IWorkContext _workContext;
        private readonly IRepository<MarketplaceBusiness> _businessRepo;

        public MarketplaceContextBadgeViewComponent(IWorkContext workContext, IRepository<MarketplaceBusiness> businessRepo)
        {
            _workContext = workContext;
            _businessRepo = businessRepo;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return Content("");

            var business = _businessRepo.Table.FirstOrDefault(x => x.VendorId == vendor.Id);
            if (business == null)
                return Content("");

            string badgeHtml = business.RoleTypeId switch
            {
                (int)MarketplaceRoleType.Supplier => "<span class='badge bg-primary'><i class='fas fa-industry'></i> Supplier Portal</span>",
                (int)MarketplaceRoleType.Reseller => "<span class='badge bg-success'><i class='fas fa-store'></i> Reseller Context</span>",
                (int)MarketplaceRoleType.Both => "<span class='badge bg-warning text-dark'><i class='fas fa-exchange-alt'></i> Hybrid Business</span>",
                _ => ""
            };

            // FIX: Added 'new' keyword to instantiate the result
            return new HtmlContentViewComponentResult(new Microsoft.AspNetCore.Html.HtmlString(
                $"<li class='nav-item d-none d-sm-inline-block' style='margin-top: 10px; margin-right: 15px;'>{badgeHtml}</li>"
            ));
        }
    }
}