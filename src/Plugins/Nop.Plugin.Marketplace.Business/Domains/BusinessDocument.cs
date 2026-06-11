using Nop.Core;

namespace Nop.Plugin.Marketplace.Business.Domains
{
    public partial class BusinessDocument : BaseEntity
    {
        public int MarketplaceBusinessId { get; set; }
        public string DocumentType { get; set; } // e.g., "TaxId", "BusinessLicense"
        public string FileUri { get; set; }
        public string MimeType { get; set; }
        public DateTime UploadedOnUtc { get; set; }
    }
}