using System.Collections.Generic;
using Nop.Plugin.Marketplace.Core.Domains;

namespace Nop.Plugin.Marketplace.Escrow.Services
{
    public static class EscrowStateMachine
    {
        private static readonly Dictionary<EscrowState, List<EscrowState>> AllowedTransitions = new()
        {
            { EscrowState.Created, new List<EscrowState> { EscrowState.Funded, EscrowState.Cancelled } },
            { EscrowState.Funded, new List<EscrowState> { EscrowState.Processing, EscrowState.Refunded } },
            { EscrowState.Processing, new List<EscrowState> { EscrowState.Shipped, EscrowState.Refunded } },
            { EscrowState.Shipped, new List<EscrowState> { EscrowState.Delivered, EscrowState.Disputed } },
            { EscrowState.Delivered, new List<EscrowState> { EscrowState.GracePeriod, EscrowState.Disputed } },
            // CHANGED: GracePeriod moves to SettlementPending, NOT fully Settled yet.
            { EscrowState.GracePeriod, new List<EscrowState> { EscrowState.SettlementPending, EscrowState.Disputed } },
            // NEW: SettlementPending waits for Wallet Confirmation
            { EscrowState.SettlementPending, new List<EscrowState> { EscrowState.Settled, EscrowState.Disputed } },
            { EscrowState.Disputed, new List<EscrowState> { EscrowState.Refunded, EscrowState.SettlementPending } }
        };

        public static bool CanTransition(EscrowState currentState, EscrowState newState)
        {
            if (AllowedTransitions.TryGetValue(currentState, out var allowed))
            {
                return allowed.Contains(newState);
            }
            return false;
        }
    }
}