using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Accounting.Domains;

namespace Nop.Plugin.Marketplace.Accounting.Services
{
    public interface IAccountingService
    {
        Task RecordTransactionAsync(JournalEntry header, IEnumerable<JournalEntryLine> lines);
        Task<GlAccount> GetAccountByCodeAsync(string accountCode);
        Task<decimal> GetAccountBalanceAsync(string accountCode);
        Task<bool> VerifyLedgerIntegrityAsync();
    }
}