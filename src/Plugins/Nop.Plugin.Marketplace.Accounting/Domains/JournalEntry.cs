using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Accounting.Domains
{
    /// <summary>
    /// A double-entry accounting record. 
    /// The sum of its JournalEntryLines' Debits MUST equal its Credits.
    /// </summary>
    public partial class JournalEntry : BaseEntity
    {
        public DateTime TransactionDateUtc { get; set; }
        public string ReferenceId { get; set; } // e.g., "ORDER_1234", "ESCROW_REL_55"
        public string Memo { get; set; }
        public string IdempotencyKey { get; set; } // Protects against double-journaling
        public DateTime CreatedOnUtc { get; set; }
    }
}