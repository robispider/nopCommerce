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
    public class SettlementAccountingConsumer : IConsumer<SettlementRequestedEvent>
    {
        private readonly IAccountingService _accountingService;

        public SettlementAccountingConsumer(IAccountingService accountingService)
        {
            _accountingService = accountingService;
        }

        public async Task HandleEventAsync(SettlementRequestedEvent eventMessage)
        {
            var escrowLiability = await _accountingService.GetAccountByCodeAsync("2000"); // Money leaving Escrow
            var vendorPayables = await _accountingService.GetAccountByCodeAsync("2010");  // Money hitting Wallets
            var platformRevenue = await _accountingService.GetAccountByCodeAsync("4000"); // Our Cut

            var totalAmount = eventMessage.SupplierAmount + eventMessage.ResellerAmount + eventMessage.PlatformFeeAmount;

            var header = new JournalEntry
            {
                TransactionDateUtc = DateTime.UtcNow,
                ReferenceId = $"ESCROW_{eventMessage.EscrowTransactionId}",
                Memo = "Escrow Settled & Funds Distributed",
                IdempotencyKey = $"GL_SETTLE_{eventMessage.EscrowTransactionId}"
            };

            var lines = new List<JournalEntryLine>
            {
                // Debit: We no longer owe this money to Escrow (reduces liability)
                new JournalEntryLine { GlAccountId = escrowLiability.Id, DebitAmount = totalAmount, CreditAmount = 0 },
                
                // Credit: We now owe the Supplier via their Wallet
                new JournalEntryLine { GlAccountId = vendorPayables.Id, DebitAmount = 0, CreditAmount = eventMessage.SupplierAmount, VendorId = eventMessage.SupplierVendorId },
                
                // Credit: We now owe the Reseller via their Wallet
                new JournalEntryLine { GlAccountId = vendorPayables.Id, DebitAmount = 0, CreditAmount = eventMessage.ResellerAmount, VendorId = eventMessage.ResellerVendorId },
                
                // Credit: The platform recognizes its commission revenue
                new JournalEntryLine { GlAccountId = platformRevenue.Id, DebitAmount = 0, CreditAmount = eventMessage.PlatformFeeAmount }
            };

            await _accountingService.RecordTransactionAsync(header, lines);
        }
    }
}