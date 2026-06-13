using Nop.Core;

namespace Nop.Plugin.Marketplace.Accounting.Domains
{
    public partial class JournalEntryLine : BaseEntity
    {
        public int JournalEntryId { get; set; }
        public int GlAccountId { get; set; }

        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }

        // Optional: Tag lines to specific entities for granular reporting
        public int? VendorId { get; set; }
        public int? OrderId { get; set; }
    }
}