using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Nop.Data;
using Nop.Plugin.Marketplace.Accounting.Domains;

namespace Nop.Plugin.Marketplace.Accounting.Services
{
    public class AccountingService : IAccountingService
    {
        private readonly IRepository<JournalEntry> _journalEntryRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;
        private readonly IRepository<GlAccount> _glAccountRepository;

        public AccountingService(
            IRepository<JournalEntry> journalEntryRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository,
            IRepository<GlAccount> glAccountRepository)
        {
            _journalEntryRepository = journalEntryRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
            _glAccountRepository = glAccountRepository;
        }

        public async Task<GlAccount> GetAccountByCodeAsync(string accountCode)
        {
            return await _glAccountRepository.Table.FirstOrDefaultAsync(x => x.AccountCode == accountCode);
        }

        public async Task RecordTransactionAsync(JournalEntry header, IEnumerable<JournalEntryLine> lines)
        {
            var lineList = lines.ToList();

            // 1. THE GOLDEN RULE OF DOUBLE-ENTRY ACCOUNTING
            var totalDebits = lineList.Sum(x => x.DebitAmount);
            var totalCredits = lineList.Sum(x => x.CreditAmount);

            if (totalDebits != totalCredits)
                throw new Exception($"FATAL ACCOUNTING ERROR: Debits ({totalDebits}) do not equal Credits ({totalCredits}). Transaction halted.");

            if (totalDebits <= 0)
                throw new Exception("Transaction amount must be greater than zero.");

            // 2. IDEMPOTENCY CHECK (Prevent Double Logging)
            if (await _journalEntryRepository.Table.AnyAsync(x => x.IdempotencyKey == header.IdempotencyKey))
                return;

            // 3. ATOMIC DATABASE COMMIT
            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

            header.CreatedOnUtc = DateTime.UtcNow;
            await _journalEntryRepository.InsertAsync(header);

            foreach (var line in lineList)
            {
                line.JournalEntryId = header.Id;
                await _journalEntryLineRepository.InsertAsync(line);
            }

            scope.Complete();
        }
    }
}