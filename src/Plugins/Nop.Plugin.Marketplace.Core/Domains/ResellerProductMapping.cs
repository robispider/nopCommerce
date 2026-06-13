using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nop.Core;

namespace Nop.Plugin.Marketplace.Core.Domains
{
    /// <summary>
    /// Maps a Reseller's "Light Cloned" product to the original Supplier product.
    /// </summary>
    public partial class ResellerProductMapping : BaseEntity
    {
        /// <summary>
        /// The native ProductId of the clone owned by the Reseller
        /// </summary>
        public int ResellerCoreProductId { get; set; }

        /// <summary>
        /// The native ProductId of the master product owned by the Supplier
        /// </summary>
        public int SupplierCoreProductId { get; set; }

        /// <summary>
        /// The Reseller's MarketplaceBusiness ID
        /// </summary>
        public int ResellerBusinessId { get; set; }

        public int SelectedProcurementPolicyId { get; set; }

        public bool SyncInventory { get; set; }

        public decimal MarginPercentage { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }
}