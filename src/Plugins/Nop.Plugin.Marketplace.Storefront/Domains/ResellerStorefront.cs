using Nop.Core;

namespace Nop.Plugin.Marketplace.Storefront.Domains
{
    /// <summary>
    /// Represents the UI/Branding configuration and URL mapping for a Reseller's storefront.
    /// </summary>
    public partial class ResellerStorefront : BaseEntity
    {
        /// <summary>
        /// Gets or sets the native nopCommerce VendorId that owns this storefront.
        /// </summary>
        public int VendorId { get; set; }

        /// <summary>
        /// Gets or sets the relative URL slug (e.g., "shoeking" for /store/shoeking).
        /// </summary>
        public string UrlSlug { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified custom domain (e.g., "www.shoeking.com").
        /// </summary>
        public string CustomDomain { get; set; }

        /// <summary>
        /// Gets or sets the display name of the store.
        /// </summary>
        public string StoreName { get; set; }

        /// <summary>
        /// Gets or sets the primary CSS hex color for dynamic branding (e.g., "#FF0000").
        /// </summary>
        public string PrimaryColorHex { get; set; }

        /// <summary>
        /// Gets or sets the PictureId from the core Picture table for the Store Logo.
        /// </summary>
        public int LogoPictureId { get; set; }

        /// <summary>
        /// Gets or sets the PictureId from the core Picture table for the Store Banner.
        /// </summary>
        public int BannerPictureId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the storefront is active and accessible.
        /// </summary>
        public bool IsActive { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime UpdatedOnUtc { get; set; }
    }
}