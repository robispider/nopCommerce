using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Accounting.Domains;
using Nop.Plugin.Marketplace.Accounting.Services;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Accounting.Consumers
{
    public class ReserveReleaseAccountingConsumer : IConsumer<ReserveReleasedEvent>
    {
        private readonly IAccountingService _accountingService;

        public ReserveReleaseAccountingConsumer(IAccountingService accountingService)
        {
            _accountingService = accountingService;
        }

        public async Task HandleEventAsync(ReserveReleasedEvent eventMessage)
        {
            var reservePayable = await _accountingService.GetAccountByCodeAsync("2020");   // Reserve Liability
            var availablePayable = await _accountingService.GetAccountByCodeAsync("2010"); // Available Liability

            var header = new JournalEntry
            {
                TransactionDateUtc = DateTime.UtcNow,
                ReferenceId = $"RELEASE_{eventMessage.ReserveScheduleId}",
                Memo = "Reserve Balance Released to Available Wallet",
                IdempotencyKey = $"GL_{eventMessage.IdempotencyKey}"
            };

            var lines = new List<JournalEntryLine>
            {
                // Debit Reserve: Reduces our reserve liability to the vendor
                new JournalEntryLine { GlAccountId = reservePayable.Id, DebitAmount = eventMessage.Amount, CreditAmount = 0, VendorId = eventMessage.VendorId },
                
                // Credit Available: Increases our immediately payable liability to the vendor
                new JournalEntryLine { GlAccountId = availablePayable.Id, DebitAmount = 0, CreditAmount = eventMessage.Amount, VendorId = eventMessage.VendorId }
            };

            await _accountingService.RecordTransactionAsync(header, lines);
        }
    }
}