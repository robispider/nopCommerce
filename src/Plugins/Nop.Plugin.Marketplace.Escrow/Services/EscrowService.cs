using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Newtonsoft.Json;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Escrow.Domains;

namespace Nop.Plugin.Marketplace.Escrow.Services
{
    public class EscrowService : IEscrowService
    {
        private readonly IRepository<EscrowTransaction> _escrowRepository;
        private readonly IRepository<OutboxMessage> _outboxRepository;
        private readonly IRepository<EscrowStateHistory> _historyRepository; // <-- Added
        private readonly ICommissionService _commissionService;

        public EscrowService(
            IRepository<EscrowTransaction> escrowRepository,
            IRepository<OutboxMessage> outboxRepository,
            IRepository<EscrowStateHistory> historyRepository, // <-- Added
            ICommissionService commissionService)
        {
            _escrowRepository = escrowRepository;
            _outboxRepository = outboxRepository;
            _historyRepository = historyRepository;
            _commissionService = commissionService;
        }

        public async Task TransitionStateByOrderIdAsync(int coreOrderId, EscrowState newState, string systemNote, int? adminUserId = null)
        {
            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

            var escrow = await _escrowRepository.Table.FirstOrDefaultAsync(x => x.CoreOrderId == coreOrderId);
            if (escrow == null || escrow.CurrentStateId == (int)newState)
                return;

            int oldStateId = escrow.CurrentStateId;

            // 1. Update State
            escrow.CurrentStateId = (int)newState;
            escrow.UpdatedOnUtc = DateTime.UtcNow;
            await _escrowRepository.UpdateAsync(escrow);

            // 2. Log History
            await _historyRepository.InsertAsync(new EscrowStateHistory
            {
                EscrowTransactionId = escrow.Id,
                OldStateId = oldStateId,
                NewStateId = (int)newState,
                SystemNote = systemNote,
                AdminUserId = adminUserId,
                CreatedOnUtc = DateTime.UtcNow
            });

            scope.Complete();
        }

        public async Task DisputeEscrowAsync(int escrowTransactionId, string reason, int adminUserId)
        {
            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

            var escrow = await _escrowRepository.GetByIdAsync(escrowTransactionId);
            if (escrow == null)
                throw new Exception("Escrow not found.");

            // Can only dispute if it hasn't been released yet
            // To this:
            if (escrow.CurrentStateId == (int)EscrowState.Settled || escrow.CurrentStateId == (int)EscrowState.SettlementPending || escrow.CurrentStateId == (int)EscrowState.Refunded)
                throw new Exception("Terminal states cannot be disputed.");

            int oldStateId = escrow.CurrentStateId;

            escrow.CurrentStateId = (int)EscrowState.Disputed;
            escrow.UpdatedOnUtc = DateTime.UtcNow;
            await _escrowRepository.UpdateAsync(escrow);

            await _historyRepository.InsertAsync(new EscrowStateHistory
            {
                EscrowTransactionId = escrow.Id,
                OldStateId = oldStateId,
                NewStateId = (int)EscrowState.Disputed,
                SystemNote = $"DISPUTED: {reason}",
                AdminUserId = adminUserId,
                CreatedOnUtc = DateTime.UtcNow
            });

            scope.Complete();
        }


        public async Task TransitionStateByOrderIdAsync(int coreOrderId, EscrowState newState, string systemNote)
        {
            var escrow = await _escrowRepository.Table.FirstOrDefaultAsync(x => x.CoreOrderId == coreOrderId);
            if (escrow == null)
                return;

            escrow.CurrentStateId = (int)newState;
            escrow.UpdatedOnUtc = DateTime.UtcNow;
            await _escrowRepository.UpdateAsync(escrow);
        }





        public async Task ReleaseFundsAsync(int escrowTransactionId, int adminUserId = 0)
        {
            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

            var escrow = await _escrowRepository.GetByIdAsync(escrowTransactionId);
            if (escrow.CurrentStateId != (int)EscrowState.GracePeriod)
                throw new Exception("Escrow is not in Grace Period.");

            var splits = await _commissionService.CalculateSplitsAsync(escrow.CoreOrderId);

            // AMAZON-GRADE: State is only "Pending Settlement", NOT Released yet.
            escrow.CurrentStateId = (int)EscrowState.SettlementPending;
            escrow.UpdatedOnUtc = DateTime.UtcNow;
            await _escrowRepository.UpdateAsync(escrow);

            // Create the Settlement Request Event
            var releaseEvent = new SettlementRequestedEvent
            {
                EscrowTransactionId = escrow.Id,
                SupplierVendorId = escrow.SupplierVendorId,
                SupplierAmount = splits.SupplierAmount,
                ResellerVendorId = escrow.ResellerVendorId,
                ResellerAmount = splits.ResellerAmount,
                PlatformFeeAmount = splits.NetPlatformFeeAmount,
                IdempotencyKey = $"SETTLE_{escrow.Id}_REQ"
            };

            await _outboxRepository.InsertAsync(new OutboxMessage
            {
                EventType = "Nop.Plugin.Marketplace.Core.Events.SettlementRequestedEvent", // Updated Event Name
                Payload = JsonConvert.SerializeObject(releaseEvent),
                CreatedOnUtc = DateTime.UtcNow
            });

            scope.Complete();
        }

        // NEW METHOD: Called when the Wallet confirms receipt of funds
        public async Task MarkAsSettledAsync(int escrowTransactionId)
        {
            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

            var escrow = await _escrowRepository.GetByIdAsync(escrowTransactionId);
            if (escrow != null && escrow.CurrentStateId == (int)EscrowState.SettlementPending)
            {
                escrow.CurrentStateId = (int)EscrowState.Settled;
                escrow.UpdatedOnUtc = DateTime.UtcNow;
                await _escrowRepository.UpdateAsync(escrow);

                await _historyRepository.InsertAsync(new EscrowStateHistory
                {
                    EscrowTransactionId = escrow.Id,
                    OldStateId = (int)EscrowState.SettlementPending,
                    NewStateId = (int)EscrowState.Settled,
                    SystemNote = "Wallet Confirmed Settlement.",
                    CreatedOnUtc = DateTime.UtcNow
                });
            }
            scope.Complete();
        }
    }
}