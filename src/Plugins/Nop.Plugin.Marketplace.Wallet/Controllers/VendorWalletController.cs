using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Marketplace.Wallet.Domains;
using Nop.Plugin.Marketplace.Wallet.Models;
using Nop.Plugin.Marketplace.Wallet.Services;
using Nop.Services.Directory;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc.Filters;
namespace Nop.Plugin.Marketplace.Wallet.Controllers
{
    // Ensure only logged-in Vendors can access this
    [AuthorizeAdmin] // In nopCommerce, Vendors use the Admin panel area
    [Area(AreaNames.ADMIN)]
    public class VendorWalletController : BasePluginController
    {
        private readonly IWorkContext _workContext;
        private readonly IRepository<WalletAccount> _accountRepository;
        private readonly IWalletTransactionService _walletTransactionService;

        private readonly IRepository<WalletLedger> _ledgerRepository;

        // 2. Update Constructor:
        public VendorWalletController(
            IWorkContext workContext,
            IRepository<WalletAccount> accountRepository,
            IRepository<WalletLedger> ledgerRepository, // <-- INJECTED HERE
            IWalletTransactionService walletTransactionService)
        {
            _workContext = workContext;
            _accountRepository = accountRepository;
            _ledgerRepository = ledgerRepository;       // <-- ASSIGNED HERE
            _walletTransactionService = walletTransactionService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return AccessDeniedView();

            var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == vendor.Id);
            if (account == null)
            {
                // Auto-create wallet if they visit for the first time
                account = new WalletAccount { VendorId = vendor.Id };
                await _accountRepository.InsertAsync(account);
            }

            var model = new WalletDashboardModel
            {
                AvailableBalance = account.AvailableBalance,
                PendingBalance = account.PendingBalance,
                ReserveBalance = account.ReserveBalance
            };
            model.LedgerSearchModel.SetGridPageSize();

            return View("~/Plugins/Marketplace.Wallet/Views/VendorWallet/Dashboard.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> LedgerList(WalletLedgerSearchModel searchModel)
        {
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return Json(new { }); // Exit early if null

            // FIXED: Removed the '?.' operator inside the Expression Tree
            var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == vendor.Id);
            if (account == null)
                return Json(new { });

            var query = _ledgerRepository.Table
                .Where(x => x.WalletAccountId == account.Id)
                .OrderByDescending(x => x.CreatedOnUtc);

            var records = await query.Skip((searchModel.Page - 1) * searchModel.PageSize)
                                     .Take(searchModel.PageSize).ToListAsync();

            // FIXED: Using WalletLedgerListModel instead of the abstract base class
            var model = new WalletLedgerListModel().PrepareToGrid(searchModel, new PagedList<WalletLedger>(records, searchModel.Page - 1, searchModel.PageSize, query.Count()), () =>
            {
                return records.Select(l => new WalletLedgerModel
                {
                    Id = l.Id,
                    EntryType = l.EntryTypeId == 1 ? "<span class='badge bg-success'>Credit</span>" : "<span class='badge bg-danger'>Debit</span>",
                    Amount = l.Amount.ToString("C"),
                    Reference = $"{l.ReferenceType} #{l.ReferenceId}",
                    Date = l.CreatedOnUtc.ToString("g")
                });
            });

            return Json(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken] // Strict CSRF protection
        public async Task<IActionResult> RequestWithdrawal(decimal amount)
        {
            var vendor = await _workContext.GetCurrentVendorAsync();
            if (vendor == null)
                return AccessDeniedView();

            try
            {
                await _walletTransactionService.RequestWithdrawalAsync(vendor.Id, amount);
                // In nopCommerce, you would normally inject INotificationService and display a success message here.
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                // Display error (e.g. "Insufficient balance")
                ModelState.AddModelError("", ex.Message);
                return await Dashboard();
            }
        }


    }
}