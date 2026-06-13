using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Escrow.Domains
{
    public partial class EscrowStateHistory : BaseEntity
    {
        public int EscrowTransactionId { get; set; }
        public int OldStateId { get; set; }
        public int NewStateId { get; set; }
        public string SystemNote { get; set; }
        public int? AdminUserId { get; set; } // Null if changed by system/automation
        public DateTime CreatedOnUtc { get; set; }
    }
}