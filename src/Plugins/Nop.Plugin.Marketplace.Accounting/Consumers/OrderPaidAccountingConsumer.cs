using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Plugin.Marketplace.Accounting.Domains;
using Nop.Plugin.Marketplace.Accounting.Services;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Accounting.Consumers
{
    public class OrderPaidAccountingConsumer : IConsumer<OrderPaidEvent>
    {
        private readonly IAccountingService _accountingService;

        public OrderPaidAccountingConsumer(IAccountingService accountingService)
        {
            _accountingService = accountingService;
        }

        public async Task HandleEventAsync(OrderPaidEvent eventMessage)
        {
            var order = eventMessage.Order;
            decimal amount = order.OrderTotal;

            // Retrieve Accounts
            var clearingAccount = await _accountingService.GetAccountByCodeAsync("1000"); // Gateway Clearing (Asset)
            var escrowLiability = await _accountingService.GetAccountByCodeAsync("2000"); // Escrow Holding (Liability)

            var header = new JournalEntry
            {
                TransactionDateUtc = DateTime.UtcNow,
                ReferenceId = $"ORDER_{order.Id}",
                Memo = "Customer Payment Collected via Gateway",
                IdempotencyKey = $"GL_ORD_PAID_{order.Id}"
            };

            var lines = new List<JournalEntryLine>
            {
                new JournalEntryLine { GlAccountId = clearingAccount.Id, DebitAmount = amount, CreditAmount = 0, OrderId = order.Id },
                new JournalEntryLine { GlAccountId = escrowLiability.Id, DebitAmount = 0, CreditAmount = amount, OrderId = order.Id }
            };

            await _accountingService.RecordTransactionAsync(header, lines);
        }
    }
}