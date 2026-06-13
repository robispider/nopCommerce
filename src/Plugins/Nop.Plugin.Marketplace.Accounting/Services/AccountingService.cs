using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Caching; // Native Nop Lock
using Nop.Data;
using Nop.Plugin.Marketplace.Accounting.Domains;

namespace Nop.Plugin.Marketplace.Accounting.Services
{
    public class AccountingService : IAccountingService
    {
        private readonly IRepository<JournalEntry> _journalEntryRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;
        private readonly IRepository<GlAccount> _glAccountRepository;
        private readonly ILocker _locker;

        public AccountingService(
            IRepository<JournalEntry> journalEntryRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository,
            IRepository<GlAccount> glAccountRepository,
            ILocker locker)
        {
            _journalEntryRepository = journalEntryRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
            _glAccountRepository = glAccountRepository;
            _locker = locker;
        }

        public async Task<GlAccount> GetAccountByCodeAsync(string accountCode)
        {
            return await _glAccountRepository.Table.FirstOrDefaultAsync(x => x.AccountCode == accountCode);
        }

        public async Task RecordTransactionAsync(JournalEntry header, IEnumerable<JournalEntryLine> lines)
        {
            var lineList = lines.ToList();

            // 1. THE GOLDEN RULE OF DOUBLE-ENTRY ACCOUNTING
            var totalDebits = Math.Round(lineList.Sum(x => x.DebitAmount), 4);
            var totalCredits = Math.Round(lineList.Sum(x => x.CreditAmount), 4);

            if (totalDebits != totalCredits)
                throw new Exception($"FATAL ACCOUNTING ERROR: Debits ({totalDebits}) do not equal Credits ({totalCredits}). Ledger unaligned.");

            if (totalDebits <= 0)
                throw new Exception("Transaction amount must be greater than zero.");

            string lockKey = $"marketplace_accounting_post_lock_{header.IdempotencyKey}";

            // 2. CONCURRENCY-SAFE LEDGER POSTING
            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
            {
                // Re-check idempotency inside lock
                if (await _journalEntryRepository.Table.AnyAsync(x => x.IdempotencyKey == header.IdempotencyKey))
                    return;

                header.CreatedOnUtc = DateTime.UtcNow;
                await _journalEntryRepository.InsertAsync(header);

                foreach (var line in lineList)
                {
                    line.JournalEntryId = header.Id;
                    await _journalEntryLineRepository.InsertAsync(line);
                }
            });
        }

        public async Task<decimal> GetAccountBalanceAsync(string accountCode)
        {
            var account = await GetAccountByCodeAsync(accountCode);
            if (account == null)
                return 0;

            var query = _journalEntryLineRepository.Table.Where(x => x.GlAccountId == account.Id);
            var totalDebits = await Task.FromResult(query.Sum(x => x.DebitAmount));
            var totalCredits = await Task.FromResult(query.Sum(x => x.CreditAmount));

            // Assets and Expenses are Debit-Normal accounts
            bool isDebitNormal = account.AccountTypeId == (int)GlAccountType.Asset ||
                                 account.AccountTypeId == (int)GlAccountType.Expense;

            return isDebitNormal ? (totalDebits - totalCredits) : (totalCredits - totalDebits);
        }

        public async Task<bool> VerifyLedgerIntegrityAsync()
        {
            // ALIBABA RECONCILIATION ENGINE: Sum(All Debits) MUST equal Sum(All Credits) globally.
            var totalDebits = await Task.FromResult(_journalEntryLineRepository.Table.Sum(x => x.DebitAmount));
            var totalCredits = await Task.FromResult(_journalEntryLineRepository.Table.Sum(x => x.CreditAmount));

            return Math.Round(totalDebits, 4) == Math.Round(totalCredits, 4);
        }
    }
}