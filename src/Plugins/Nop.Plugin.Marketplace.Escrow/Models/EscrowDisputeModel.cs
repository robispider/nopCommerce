using Nop.Web.Framework.Models;

namespace Nop.Plugin.Marketplace.Escrow.Models
{
    public record EscrowDisputeSearchModel : BaseSearchModel { }

    public record EscrowDisputeModel : BaseNopEntityModel
    {
        public string OrderNumber { get; set; }
        public string SupplierName { get; set; }
        public string ResellerName { get; set; }
        public string DateDisputed { get; set; }
    }
    public record EscrowDisputeListModel : BasePagedListModel<EscrowDisputeModel> { }
}