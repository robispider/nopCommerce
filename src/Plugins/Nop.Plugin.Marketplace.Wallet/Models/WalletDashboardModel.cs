using Nop.Web.Framework.Models;

namespace Nop.Plugin.Marketplace.Wallet.Models
{
    public record WalletDashboardModel : BaseNopModel
    {
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal ReserveBalance { get; set; }
        public WalletLedgerSearchModel LedgerSearchModel { get; set; } = new WalletLedgerSearchModel();
    }

    public record WalletLedgerSearchModel : BaseSearchModel { }

    public record WalletLedgerModel : BaseNopEntityModel
    {
        public string EntryType { get; set; } // Credit or Debit
        public string Amount { get; set; }
        public string Reference { get; set; }
        public string Date { get; set; }
    }
    public record WalletLedgerListModel : BasePagedListModel<WalletLedgerModel> { }
}