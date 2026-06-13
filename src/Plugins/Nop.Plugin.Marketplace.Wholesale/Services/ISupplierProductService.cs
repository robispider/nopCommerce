using System.Threading.Tasks;
using Nop.Core;
using Nop.Plugin.Marketplace.Wholesale.Domains;

namespace Nop.Plugin.Marketplace.Wholesale.Services
{
    public interface ISupplierProductService
    {
        Task<SupplierProduct> GetByProductIdAsync(int productId);
        Task InsertSupplierProductAsync(SupplierProduct supplierProduct);
        Task UpdateSupplierProductAsync(SupplierProduct supplierProduct);

        // NEW: For the B2B Sourcing Portal
        Task<IPagedList<SupplierProduct>> SearchB2BProductsAsync(int excludeVendorId, int pageIndex = 0, int pageSize = int.MaxValue);
    }
}