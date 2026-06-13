using System.Threading.Tasks;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Marketplace.Dropship.Domains;

namespace Nop.Plugin.Marketplace.Dropship.Services
{
    public class DropshipFulfillmentService : IDropshipFulfillmentService
    {
        private readonly IRepository<DropshipFulfillment> _fulfillmentRepository;

        public DropshipFulfillmentService(IRepository<DropshipFulfillment> fulfillmentRepository)
        {
            _fulfillmentRepository = fulfillmentRepository;
        }

        public async Task InsertFulfillmentAsync(DropshipFulfillment fulfillment)
        {
            await _fulfillmentRepository.InsertAsync(fulfillment);
        }
        public async Task<DropshipFulfillment> GetByIdAsync(int id)
        {
            return await _fulfillmentRepository.GetByIdAsync(id);
        }

        public async Task UpdateFulfillmentAsync(DropshipFulfillment fulfillment)
        {
            await _fulfillmentRepository.UpdateAsync(fulfillment);
        }

        public async Task<IPagedList<DropshipFulfillment>> SearchSupplierTicketsAsync(int supplierVendorId, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from f in _fulfillmentRepository.Table
                        where f.SupplierVendorId == supplierVendorId
                        orderby f.CreatedOnUtc descending
                        select f;

            // Materialize for nopCommerce v4.90 PagedList
            var totalCount = query.Count();
            var records = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();

            return await Task.FromResult(new PagedList<DropshipFulfillment>(records, pageIndex, pageSize, totalCount));
        }
    }
}