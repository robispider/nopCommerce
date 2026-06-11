namespace Nop.Plugin.Marketplace.Business.Services
{
    public interface IMarketplaceCatalogService
    {
        /// <summary>
        /// Clones a supplier's product into the current reseller's catalog with a markup.
        /// </summary>
        Task ImportSupplierProductAsync(int supplierProductId, decimal retailMarginPercentage);
    }
}