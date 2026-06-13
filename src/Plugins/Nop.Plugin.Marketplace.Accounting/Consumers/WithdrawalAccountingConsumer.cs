using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Accounting.Domains;
using Nop.Plugin.Marketplace.Accounting.Services;
using Nop.Plugin.Marketplace.Core.Events; // Assuming WithdrawalApprovedEvent exists here
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Accounting.Consumers
{
    public class WithdrawalAccountingConsumer : IConsumer<WithdrawalApprovedEvent>
    {
        private readonly IAccountingService _accountingService;

        public WithdrawalAccountingConsumer(IAccountingService accountingService)
        {
            _accountingService = accountingService;
        }

        public async Task HandleEventAsync(WithdrawalApprovedEvent eventMessage)
        {
            var vendorPayables = await _accountingService.GetAccountByCodeAsync("2010");  // Payables
            var bankAccount = await _accountingService.GetAccountByCodeAsync("1010");     // Corporate Bank Asset

            var header = new JournalEntry
            {
                TransactionDateUtc = DateTime.UtcNow,
                ReferenceId = $"WITHDRAWAL_{eventMessage.WithdrawalId}",
                Memo = $"Vendor Cash-out Processed via {eventMessage.PaymentMethod}",
                IdempotencyKey = $"GL_WD_APP_{eventMessage.WithdrawalId}"
            };

            var lines = new List<JournalEntryLine>
            {
                // Debit Payables: Reduces our liability to the vendor (we paid them)
                new JournalEntryLine { GlAccountId = vendorPayables.Id, DebitAmount = eventMessage.Amount, CreditAmount = 0, VendorId = eventMessage.VendorId },
                
                // Credit Bank: Cash asset leaves our bank account
                new JournalEntryLine { GlAccountId = bankAccount.Id, DebitAmount = 0, CreditAmount = eventMessage.Amount }
            };

            await _accountingService.RecordTransactionAsync(header, lines);
        }
    }
}