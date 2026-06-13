using Nop.Core;

namespace Nop.Plugin.Marketplace.Wallet.Domains
{
    public partial class WalletAccount : BaseEntity
    {
        public int VendorId { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal ReserveBalance { get; set; }
        public int ConcurrencyVersion { get; set; }
    }
}