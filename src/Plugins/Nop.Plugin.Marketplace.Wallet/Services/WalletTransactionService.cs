using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Caching; // NopCommerce Native Distributed Lock!
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Wallet.Domains;
using Nop.Services.Events;

namespace Nop.Plugin.Marketplace.Wallet.Services
{
    public class WalletTransactionService : IWalletTransactionService
    {
        private readonly IRepository<WalletLedger> _ledgerRepository;
        private readonly IRepository<WalletAccount> _accountRepository;
        private readonly IRepository<WithdrawalRequest> _withdrawalRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILocker _locker;

        public WalletTransactionService(
            IRepository<WalletLedger> ledgerRepository,
            IRepository<WalletAccount> accountRepository,
            IRepository<WithdrawalRequest> withdrawalRepository,
            IEventPublisher eventPublisher,
            ILocker locker)
        {
            _ledgerRepository = ledgerRepository;
            _accountRepository = accountRepository;
            _withdrawalRepository = withdrawalRepository;
            _eventPublisher = eventPublisher;
            _locker = locker;
        }

        public async Task ProcessSettlementAsync(SettlementRequestedEvent releaseEvent)
        {
            // Global Idempotency Check
            if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == releaseEvent.IdempotencyKey))
                return;

            // Credit Supplier safely
            await CreditWalletSafelyAsync(releaseEvent.SupplierVendorId, releaseEvent.SupplierAmount,
                releaseEvent.IdempotencyKey + "_SUP", "Settlement", releaseEvent.EscrowTransactionId);

            // Credit Reseller (if applicable)
            if (releaseEvent.ResellerVendorId > 0 && releaseEvent.ResellerAmount > 0)
            {
                await CreditWalletSafelyAsync(releaseEvent.ResellerVendorId, releaseEvent.ResellerAmount,
                    releaseEvent.IdempotencyKey + "_RES", "Settlement", releaseEvent.EscrowTransactionId);
            }

            // Tell Escrow we finished so it can move to "Settled"
            await _eventPublisher.PublishAsync(new WalletSettledEvent
            {
                EscrowTransactionId = releaseEvent.EscrowTransactionId,
                IdempotencyKey = releaseEvent.IdempotencyKey
            });
        }

        public async Task ProcessRefundAsync(EscrowRefundedEvent refundEvent)
        {
            if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == refundEvent.IdempotencyKey))
                return;

            // Return the Reseller's deposit/prepay
            await CreditWalletSafelyAsync(refundEvent.ResellerVendorId, refundEvent.RefundAmount,
                refundEvent.IdempotencyKey, "Refund", refundEvent.EscrowTransactionId);
        }

        private async Task CreditWalletSafelyAsync(int vendorId, decimal amount, string idempotencyKey, string refType, int refId)
        {
            if (amount <= 0)
                return;

            string lockKey = $"marketplace_wallet_lock_{vendorId}";

            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
            {
                // Re-check idempotency inside the lock
                if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == idempotencyKey))
                    return;

                var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == vendorId);
                if (account == null)
                    throw new Exception($"Wallet missing for Vendor {vendorId}.");

                await _ledgerRepository.InsertAsync(new WalletLedger
                {
                    WalletAccountId = account.Id,
                    EntryTypeId = (int)LedgerEntryType.Credit,
                    Amount = amount,
                    ReferenceType = refType,
                    ReferenceId = refId,
                    IdempotencyKey = idempotencyKey,
                    CreatedOnUtc = DateTime.UtcNow
                });

                account.AvailableBalance += amount;
                account.ConcurrencyVersion += 1;
                await _accountRepository.UpdateAsync(account);
            });
        }

        public async Task<int> RequestWithdrawalAsync(int vendorId, decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            int requestId = 0;
            string lockKey = $"marketplace_wallet_lock_{vendorId}";

            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
            {
                var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == vendorId);
                if (account == null || account.AvailableBalance < amount)
                    throw new Exception("Insufficient available balance.");

                // Freeze the funds into Pending
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
                requestId = request.Id;
            });

            return requestId;
        }

        public async Task ApproveWithdrawalAsync(int withdrawalRequestId, string adminNotes = null)
        {
            var request = await _withdrawalRepository.GetByIdAsync(withdrawalRequestId);
            if (request == null || request.StatusId != (int)WithdrawalStatus.Requested)
                throw new Exception("Invalid or already processed withdrawal request.");

            string lockKey = $"marketplace_wallet_lock_{request.VendorId}";

            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
            {
                var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == request.VendorId);
                string idempotencyKey = $"WD_{request.Id}_APP";

                if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == idempotencyKey))
                    return;

                // 1. Money leaves the platform permanently
                await _ledgerRepository.InsertAsync(new WalletLedger
                {
                    WalletAccountId = account.Id,
                    EntryTypeId = (int)LedgerEntryType.Debit,
                    Amount = request.Amount,
                    ReferenceType = "Withdrawal",
                    ReferenceId = request.Id,
                    IdempotencyKey = idempotencyKey,
                    CreatedOnUtc = DateTime.UtcNow
                });

                account.PendingBalance -= request.Amount;
                account.ConcurrencyVersion += 1;
                await _accountRepository.UpdateAsync(account);

                request.StatusId = (int)WithdrawalStatus.Completed;
                request.AdminNotes = adminNotes;
                request.UpdatedOnUtc = DateTime.UtcNow;
                await _withdrawalRepository.UpdateAsync(request);

                // 2. ALIBABA-GRADE: Notify the Accounting ledger!
                await _eventPublisher.PublishAsync(new WithdrawalApprovedEvent
                {
                    WithdrawalId = request.Id,
                    VendorId = request.VendorId,
                    Amount = request.Amount,
                    PaymentMethod = "Bank Transfer" // Standard fallback, can be populated from request data
                });
            });
        }
        public async Task RejectWithdrawalAsync(int withdrawalRequestId, string adminNotes = null)
        {
            var request = await _withdrawalRepository.GetByIdAsync(withdrawalRequestId);
            if (request == null || request.StatusId != (int)WithdrawalStatus.Requested)
                throw new Exception("Invalid or already processed withdrawal request.");

            string lockKey = $"marketplace_wallet_lock_{request.VendorId}";

            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
            {
                var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == request.VendorId);

                // Return funds back to Available Balance from Pending Balance
                account.PendingBalance -= request.Amount;
                account.AvailableBalance += request.Amount;
                account.ConcurrencyVersion += 1;
                await _accountRepository.UpdateAsync(account);

                request.StatusId = (int)WithdrawalStatus.Rejected;
                request.AdminNotes = adminNotes;
                request.UpdatedOnUtc = DateTime.UtcNow;
                await _withdrawalRepository.UpdateAsync(request);
            });
        }

        public async Task ProcessReserveHoldAsync(ReserveHoldRequestedEvent holdEvent)
        {
            if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == holdEvent.IdempotencyKey))
                return;

            string lockKey = $"marketplace_wallet_lock_{holdEvent.VendorId}";

            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
            {
                if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == holdEvent.IdempotencyKey))
                    return;

                var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == holdEvent.VendorId);
                if (account == null)
                    throw new Exception($"Wallet missing for Vendor {holdEvent.VendorId}.");

                // Move from Available -> Reserve
                account.AvailableBalance -= holdEvent.Amount;
                account.ReserveBalance += holdEvent.Amount;
                account.ConcurrencyVersion += 1;
                await _accountRepository.UpdateAsync(account);

                await _ledgerRepository.InsertAsync(new WalletLedger
                {
                    WalletAccountId = account.Id,
                    EntryTypeId = (int)LedgerEntryType.Debit,
                    Amount = holdEvent.Amount,
                    ReferenceType = "ReserveHold",
                    ReferenceId = holdEvent.EscrowTransactionId,
                    IdempotencyKey = holdEvent.IdempotencyKey,
                    CreatedOnUtc = DateTime.UtcNow
                });
            });
        }

        public async Task ProcessReserveReleaseAsync(ReserveReleasedEvent releaseEvent)
        {
            if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == releaseEvent.IdempotencyKey))
                return;

            string lockKey = $"marketplace_wallet_lock_{releaseEvent.VendorId}";

            await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
            {
                if (await _ledgerRepository.Table.AnyAsync(x => x.IdempotencyKey == releaseEvent.IdempotencyKey))
                    return;

                var account = await _accountRepository.Table.FirstOrDefaultAsync(x => x.VendorId == releaseEvent.VendorId);
                if (account == null)
                    throw new Exception($"Wallet missing for Vendor {releaseEvent.VendorId}.");

                // Move from Reserve -> Available
                account.ReserveBalance -= releaseEvent.Amount;
                account.AvailableBalance += releaseEvent.Amount;
                account.ConcurrencyVersion += 1;
                await _accountRepository.UpdateAsync(account);

                await _ledgerRepository.InsertAsync(new WalletLedger
                {
                    WalletAccountId = account.Id,
                    EntryTypeId = (int)LedgerEntryType.Credit,
                    Amount = releaseEvent.Amount,
                    ReferenceType = "ReserveRelease",
                    ReferenceId = releaseEvent.ReserveScheduleId,
                    IdempotencyKey = releaseEvent.IdempotencyKey,
                    CreatedOnUtc = DateTime.UtcNow
                });
            });
        }
    }
}