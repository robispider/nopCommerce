using Nop.Core;

namespace Nop.Plugin.Marketplace.Accounting.Domains
{
    /// <summary>
    /// Represents an account in the Chart of Accounts (CoA).
    /// </summary>
    public partial class GlAccount : BaseEntity
    {
        public string AccountCode { get; set; } // e.g. "1001" for Cash, "2001" for Escrow
        public string Name { get; set; }
        public int AccountTypeId { get; set; }
        public bool IsActive { get; set; }
    }
}