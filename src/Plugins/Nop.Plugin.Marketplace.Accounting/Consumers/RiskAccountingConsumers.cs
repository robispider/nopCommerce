using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Events;
using Nop.Plugin.Marketplace.Accounting.Domains;
using Nop.Plugin.Marketplace.Accounting.Services;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Accounting.Consumers
{
    public class ReserveHoldAccountingConsumer : IConsumer<ReserveHoldRequestedEvent>
    {
        private readonly IAccountingService _accountingService;
        public ReserveHoldAccountingConsumer(IAccountingService accountingService) { _accountingService = accountingService; }

        public async Task HandleEventAsync(ReserveHoldRequestedEvent eventMessage)
        {
            var availablePayable = await _accountingService.GetAccountByCodeAsync("2010"); // Available Payables
            var reservePayable = await _accountingService.GetAccountByCodeAsync("2020");   // Reserve Payables

            var header = new JournalEntry { TransactionDateUtc = DateTime.UtcNow, ReferenceId = $"HOLD_{eventMessage.EscrowTransactionId}", IdempotencyKey = $"GL_{eventMessage.IdempotencyKey}" };

            var lines = new List<JournalEntryLine> {
                new JournalEntryLine { GlAccountId = availablePayable.Id, DebitAmount = eventMessage.Amount, CreditAmount = 0, VendorId = eventMessage.VendorId }, // Reduce Available Liability
                new JournalEntryLine { GlAccountId = reservePayable.Id, DebitAmount = 0, CreditAmount = eventMessage.Amount, VendorId = eventMessage.VendorId }    // Increase Reserve Liability
            };

            await _accountingService.RecordTransactionAsync(header, lines);
        }
    }
}