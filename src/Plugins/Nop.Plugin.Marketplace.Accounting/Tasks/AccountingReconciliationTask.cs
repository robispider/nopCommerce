using System;
using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Plugin.Marketplace.Accounting.Services;
using Nop.Services.Logging; // NopCommerce Logger
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Marketplace.Accounting.Tasks
{
    /// <summary>
    /// Background cron job that verifies the platform's double-entry ledger integrity.
    /// If debits do not equal credits globally, a critical system error is raised.
    /// </summary>
    public class AccountingReconciliationTask : IScheduleTask
    {
        private readonly IAccountingService _accountingService;
        private readonly ILogger _logger;
        private readonly ILocker _locker;

        public AccountingReconciliationTask(
            IAccountingService accountingService,
            ILogger logger,
            ILocker locker)
        {
            _accountingService = accountingService;
            _logger = logger;
            _locker = locker;
        }

        public async Task ExecuteAsync()
        {
            string lockKey = "marketplace_accounting_global_reconciliation_lock";

            // Ensure only one instance of the audit runs globally
            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromMinutes(5), async () =>
            {
                try
                {
                    // Calculate Sum(All Debits) == Sum(All Credits)
                    bool isLedgerValid = await _accountingService.VerifyLedgerIntegrityAsync();

                    if (!isLedgerValid)
                    {
                        // Retrieve current account balances for logging diagnostic data
                        decimal bankBalance = await _accountingService.GetAccountBalanceAsync("1010");
                        decimal escrowBalance = await _accountingService.GetAccountBalanceAsync("2000");
                        decimal payableBalance = await _accountingService.GetAccountBalanceAsync("2010");

                        // ALIBABA-GRADE: Log as Critical Error. 
                        // This should trigger automated alerting (PagerDuty, SMS, or Admin UI alerts).
                        await _logger.ErrorAsync(
                            message: "CRITICAL FINANCIAL INTEGRITY FAILURE: General Ledger is out of balance. Debits do not equal Credits. Settlements must be suspended immediately.",
                            exception: new Exception($"Diagnostic Snapshots - Bank: {bankBalance:C}, Escrow: {escrowBalance:C}, Payables: {payableBalance:C}"),
                            customer: null
                        );
                    }
                }
                catch (Exception ex)
                {
                    await _logger.ErrorAsync("Error occurred during general ledger reconciliation task execution.", ex);
                }
            });
        }
    }
}