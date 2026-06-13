using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions; // Standard .NET Transaction Scope
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Wallet.Domains;

namespace Nop.Plugin.Marketplace.Wallet.Services
{
    public class WalletTransactionService : IWalletTransactionService
    {
        private readonly IRepository<WalletLedger> _ledgerRepository;
        private readonly IRepository<WalletAccount> _accountRepository;
        private readonly IRepository<WithdrawalRequest> _withdrawalRepository;
        private readonly IEventPublisher _eventPublisher; // <-- Added

        public WalletTransactionService(
            IRepository<WalletLedger> ledgerRepository,
            IRepository<WalletAccount> accountRepository,
            IRepository<WithdrawalRequest> withdrawalRepository,
            IEventPublisher eventPublisher)
        {
            _ledgerRepository = ledgerRepository;
            _accountRepository = accountRepository;
            _withdrawalRepository = withdrawalRepository;
            _eventPublisher = eventPublisher;
        }

        public async Task ProcessSettlementRequestAsync(SettlementRequestedEvent releaseEvent)
        {
            if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == releaseEvent.IdempotencyKey))
                return;

            // Bank-Grade ACID Lock
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }, TransactionScopeAsyncFlowOption.Enabled))
            {
                await CreditWalletAsync(releaseEvent.SupplierVendorId, releaseEvent.SupplierAmount, releaseEvent.IdempotencyKey + "_SUP");
                await CreditWalletAsync(releaseEvent.ResellerVendorId, releaseEvent.ResellerAmount, releaseEvent.IdempotencyKey + "_RES");
                scope.Complete();
            } // DB is committed here.

            // AMAZON-GRADE: Tell Escrow that we safely got the money.
            // Publishing this *after* the scope completes ensures we only notify Escrow if DB write succeeds.
            await _eventPublisher.PublishAsync(new WalletSettledEvent
            {
                EscrowTransactionId = releaseEvent.EscrowTransactionId,
                IdempotencyKey = releaseEvent.IdempotencyKey
            });
        }

        private async Task CreditWalletAsync(int vendorId, decimal amount, string idempotencyKey)
        {
            var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == vendorId);
            if (account == null)
                throw new Exception("Wallet missing.");

            await _ledgerRepository.InsertAsync(new WalletLedger
            {
                WalletAccountId = account.Id,
                EntryTypeId = (int)LedgerEntryType.Credit, // NEW: Added Type to track Credits properly
                Amount = amount,
                ReferenceType = "Settlement",
                ReferenceId = 0,
                IdempotencyKey = idempotencyKey,
                CreatedOnUtc = DateTime.UtcNow
            });

            account.AvailableBalance += amount;
            account.ConcurrencyVersion += 1;
            await _accountRepository.UpdateAsync(account);
        }

        public async Task<int> RequestWithdrawalAsync(int vendorId, decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            // AMAZON-GRADE: IsolationLevel.Serializable creates a hard DB lock.
            // Prevents race conditions where 2 concurrent requests pass the balance check.
            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }, TransactionScopeAsyncFlowOption.Enabled);

            var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == vendorId);
            if (account == null || account.AvailableBalance < amount)
                throw new Exception("Insufficient available balance.");

            account.AvailableBalance -= amount;
            account.PendingBalance += amount;
            account.ConcurrencyVersion += 1;
            await _accountRepository.UpdateAsync(account);

            var request = new WithdrawalRequest
            {
                VendorId = vendorId,
                Amount = amount,
                StatusId = (int)WithdrawalStatus.Requested,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await _withdrawalRepository.InsertAsync(request);

            scope.Complete();
            return request.Id;
        }

        public async Task ProcessEscrowReleaseAsync(SettlementRequestedEvent releaseEvent)
        {
            // Fix 1: Removed .TableNoTracking (Use .Table in Nop 4.90)
            if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == releaseEvent.IdempotencyKey))
                return;

            // Fix 2: Use TransactionScope with AsyncFlowOption enabled for EF Core safely
            using var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                TransactionScopeAsyncFlowOption.Enabled);

            // Credit Supplier
            await CreditWalletAsync(releaseEvent.SupplierVendorId, releaseEvent.SupplierAmount, releaseEvent.IdempotencyKey + "_SUP");

            // Credit Reseller
            await CreditWalletAsync(releaseEvent.ResellerVendorId, releaseEvent.ResellerAmount, releaseEvent.IdempotencyKey + "_RES");

            scope.Complete();
        }

        

        public async Task ApproveWithdrawalAsync(int withdrawalRequestId, string adminNotes = null)
        {
            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

            var request = await _withdrawalRepository.GetByIdAsync(withdrawalRequestId);
            if (request == null || request.StatusId != (int)WithdrawalStatus.Requested)
                throw new Exception("Invalid or already processed withdrawal request.");

            var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == request.VendorId);

            // 1. Write Debit to Immutable Ledger (Money officially leaves platform)
            await _ledgerRepository.InsertAsync(new WalletLedger
            {
                WalletAccountId = account.Id,
                EntryTypeId = (int)LedgerEntryType.Debit,
                Amount = request.Amount,
                ReferenceType = "Withdrawal",
                ReferenceId = request.Id,
                IdempotencyKey = $"WD_{request.Id}_APP",
                CreatedOnUtc = DateTime.UtcNow
            });

            // 2. Remove funds from Pending permanently
            account.PendingBalance -= request.Amount;
            account.ConcurrencyVersion += 1;
            await _accountRepository.UpdateAsync(account);

            // 3. Mark request as Completed
            request.StatusId = (int)WithdrawalStatus.Completed;
            request.AdminNotes = adminNotes;
            request.UpdatedOnUtc = DateTime.UtcNow;
            await _withdrawalRepository.UpdateAsync(request);

            scope.Complete();
        }

        public async Task RejectWithdrawalAsync(int withdrawalRequestId, string adminNotes = null)
        {
            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

            var request = await _withdrawalRepository.GetByIdAsync(withdrawalRequestId);
            if (request == null || request.StatusId != (int)WithdrawalStatus.Requested)
                throw new Exception("Invalid or already processed withdrawal request.");

            var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == request.VendorId);

            // 1. Return funds back to Available (Unlock them)
            account.PendingBalance -= request.Amount;
            account.AvailableBalance += request.Amount;
            account.ConcurrencyVersion += 1;
            await _accountRepository.UpdateAsync(account);

            // 2. Mark request as Rejected
            request.StatusId = (int)WithdrawalStatus.Rejected;
            request.AdminNotes = adminNotes;
            request.UpdatedOnUtc = DateTime.UtcNow;
            await _withdrawalRepository.UpdateAsync(request);

            scope.Complete();
        }

        public async Task ProcessReserveHoldAsync(ReserveHoldRequestedEvent holdEvent)
        {
            if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == holdEvent.IdempotencyKey))
                return;

            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }, TransactionScopeAsyncFlowOption.Enabled);

            var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == holdEvent.VendorId);

            // Move from Available -> Reserve
            account.AvailableBalance -= holdEvent.Amount;
            account.ReserveBalance += holdEvent.Amount;
            account.ConcurrencyVersion += 1;
            await _accountRepository.UpdateAsync(account);

            await _ledgerRepository.InsertAsync(new WalletLedger
            {
                WalletAccountId = account.Id,
                EntryTypeId = 2,
                Amount = holdEvent.Amount,
                ReferenceType = "ReserveHold",
                IdempotencyKey = holdEvent.IdempotencyKey,
                CreatedOnUtc = DateTime.UtcNow
            });

            scope.Complete();
        }

        public async Task ProcessReserveReleaseAsync(ReserveReleasedEvent releaseEvent)
        {
            if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == releaseEvent.IdempotencyKey))
                return;

            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }, TransactionScopeAsyncFlowOption.Enabled);

            var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == releaseEvent.VendorId);

            // Move from Reserve -> Available
            account.ReserveBalance -= releaseEvent.Amount;
            account.AvailableBalance += releaseEvent.Amount;
            account.ConcurrencyVersion += 1;
            await _accountRepository.UpdateAsync(account);

            await _ledgerRepository.InsertAsync(new WalletLedger
            {
                WalletAccountId = account.Id,
                EntryTypeId = 1,
                Amount = releaseEvent.Amount,
                ReferenceType = "ReserveRelease",
                IdempotencyKey = releaseEvent.IdempotencyKey,
                CreatedOnUtc = DateTime.UtcNow
            });

            scope.Complete();
        }
    }
}