using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.Escrow.Domains
{
    public partial class EscrowTransaction : BaseEntity
    {
        public int CoreOrderId { get; set; }
        public int SupplierVendorId { get; set; }
        public int ResellerVendorId { get; set; }
        public int CurrentStateId { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
    }
}