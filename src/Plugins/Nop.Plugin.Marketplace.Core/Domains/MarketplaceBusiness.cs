using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




using Nop.Core;

namespace Nop.Plugin.Marketplace.Core.Domains
{
    /// <summary>
    /// Represents the ultimate legal entity using the marketplace.
    /// </summary>
    public partial class MarketplaceBusiness : BaseEntity
    {
        /// <summary>
        /// Gets or sets the native nopCommerce VendorId
        /// </summary>
        public int VendorId { get; set; }

        public string LegalName { get; set; }

        public string TaxId { get; set; }

        /// <summary>
        /// Gets or sets the verification status identifier (maps to BusinessVerificationStatus)
        /// </summary>
        public int VerificationStatusId { get; set; }

        /// <summary>
        /// Gets or sets the business role type identifier (maps to MarketplaceRoleType)
        /// </summary>
        public int RoleTypeId { get; set; }

        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
    }
}