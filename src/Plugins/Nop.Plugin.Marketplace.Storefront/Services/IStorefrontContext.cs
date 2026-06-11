using Nop.Plugin.Marketplace.Storefront.Domains;

namespace Nop.Plugin.Marketplace.Storefront.Services
{
    public interface IStorefrontContext
    {
        /// <summary>
        /// Sniffs the current HTTP Request URL, extracts the Reseller slug or custom domain, 
        /// and returns the associated Storefront configuration.
        /// </summary>
        Task<ResellerStorefront> GetCurrentStorefrontAsync();
    }
}