using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Wallet.Domains
{
    public partial class WalletLedger : BaseEntity
    {
        public int WalletAccountId { get; set; }
        public int EntryTypeId { get; set; }
        public decimal Amount { get; set; }
        public string ReferenceType { get; set; }
        public int ReferenceId { get; set; }
        public string IdempotencyKey { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}