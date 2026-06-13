using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Accounting.Domains;
using Nop.Plugin.Marketplace.Accounting.Services;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Accounting.Consumers
{
    public class RefundAccountingConsumer : IConsumer<EscrowRefundedEvent>
    {
        private readonly IAccountingService _accountingService;

        public RefundAccountingConsumer(IAccountingService accountingService)
        {
            _accountingService = accountingService;
        }

        public async Task HandleEventAsync(EscrowRefundedEvent eventMessage)
        {
            var escrowLiability = await _accountingService.GetAccountByCodeAsync("2000"); // Money leaving Escrow
            var vendorPayables = await _accountingService.GetAccountByCodeAsync("2010");  // Money returning to Reseller

            var header = new JournalEntry
            {
                TransactionDateUtc = DateTime.UtcNow,
                ReferenceId = $"REFUND_{eventMessage.EscrowTransactionId}",
                Memo = "Escrow Refunded. Deposit Returned to Reseller Wallet.",
                IdempotencyKey = $"GL_{eventMessage.IdempotencyKey}"
            };

            var lines = new List<JournalEntryLine>
            {
                // Debit Escrow: Reduces our outstanding escrow liability
                new JournalEntryLine { GlAccountId = escrowLiability.Id, DebitAmount = eventMessage.RefundAmount, CreditAmount = 0 },
                
                // Credit Payables: Increases our available payable liability to the Reseller
                new JournalEntryLine { GlAccountId = vendorPayables.Id, DebitAmount = 0, CreditAmount = eventMessage.RefundAmount, VendorId = eventMessage.ResellerVendorId }
            };

            await _accountingService.RecordTransactionAsync(header, lines);
        }
    }
}