// Services/SupplierProductService.cs
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Marketplace.Wholesale.Domains;

namespace Nop.Plugin.Marketplace.Wholesale.Services
{
    public class SupplierProductService : ISupplierProductService
    {
        private readonly IRepository<SupplierProduct> _supplierProductRepository;

        public SupplierProductService(IRepository<SupplierProduct> supplierProductRepository)
        {
            _supplierProductRepository = supplierProductRepository;
        }

        public async Task<SupplierProduct> GetByProductIdAsync(int productId)
        {
            var query = from sp in _supplierProductRepository.Table
                        where sp.ProductId == productId
                        select sp;

            return await Task.FromResult(query.FirstOrDefault());
        }

        public async Task InsertSupplierProductAsync(SupplierProduct supplierProduct)
        {
            await _supplierProductRepository.InsertAsync(supplierProduct);
        }

        public async Task UpdateSupplierProductAsync(SupplierProduct supplierProduct)
        {
            await _supplierProductRepository.UpdateAsync(supplierProduct);
        }
        public async Task<IPagedList<SupplierProduct>> SearchB2BProductsAsync(int excludeVendorId, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            // Get all supplier products that are NOT owned by the current logged-in reseller
            var query = from sp in _supplierProductRepository.Table
                        where sp.VendorId != excludeVendorId
                        orderby sp.Id descending
                        select sp;

            // Materialize query into list with explicit skip/take to satisfy IList requirement
            var totalCount = query.Count();
            var records = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();

            return await Task.FromResult(new PagedList<SupplierProduct>(records, pageIndex, pageSize, totalCount));
        }
    }
}