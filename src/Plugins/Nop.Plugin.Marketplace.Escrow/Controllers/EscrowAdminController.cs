using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Escrow.Domains;
using Nop.Plugin.Marketplace.Escrow.Models;
using Nop.Plugin.Marketplace.Escrow.Services;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Marketplace.Escrow.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public class EscrowAdminController : BasePluginController
    {
        private readonly IRepository<EscrowTransaction> _escrowRepository;
        private readonly IEscrowService _escrowService;
        private readonly IWorkContext _workContext;

        public EscrowAdminController(
            IRepository<EscrowTransaction> escrowRepository,
            IEscrowService escrowService,
            IWorkContext workContext)
        {
            _escrowRepository = escrowRepository;
            _escrowService = escrowService;
            _workContext = workContext;
        }

        public IActionResult DisputedList()
        {
            var model = new EscrowDisputeSearchModel();
            model.SetGridPageSize();
            return View("~/Plugins/Marketplace.Escrow/Views/EscrowAdmin/DisputedList.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> DisputedListData(EscrowDisputeSearchModel searchModel)
        {
            // Ensure only Global Admins can access
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor != null)
                return Json(new { });

            var query = _escrowRepository.Table
                .Where(e => e.CurrentStateId == (int)EscrowState.Disputed)
                .OrderBy(e => e.UpdatedOnUtc);

            var records = await query.Skip((searchModel.Page - 1) * searchModel.PageSize)
                                     .Take(searchModel.PageSize).ToListAsync();
            var model = new EscrowDisputeListModel().PrepareToGrid(searchModel, new PagedList<EscrowTransaction>(records, searchModel.Page - 1, searchModel.PageSize, query.Count()), () =>
            
            {
                return records.Select(e => new EscrowDisputeModel
                {
                    Id = e.Id,
                    OrderNumber = $"Order #{e.CoreOrderId}", // Inject IOrderService to get real CustomOrderNumber
                    SupplierName = $"Vendor #{e.SupplierVendorId}",
                    ResellerName = $"Vendor #{e.ResellerVendorId}",
                    DateDisputed = e.UpdatedOnUtc.ToString("g")
                });
            });

            return Json(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceRelease(int escrowId)
        {
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor != null)
                return AccessDeniedView();

            var currentCustomer = await _workContext.GetCurrentCustomerAsync();

            try
            {
                // Admin manually forces the payout, overriding the dispute
                await _escrowService.TransitionStateByOrderIdAsync(escrowId, EscrowState.GracePeriod, "Admin overridden. Forcing release.");
                await _escrowService.ReleaseFundsAsync(escrowId, currentCustomer.Id);
            }
            catch { /* Log error */ }

            return RedirectToAction("DisputedList");
        }
    }
}