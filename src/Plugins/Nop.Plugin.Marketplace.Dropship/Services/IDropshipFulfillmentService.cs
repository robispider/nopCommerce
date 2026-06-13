using System.Threading.Tasks;
using Nop.Core;
using Nop.Plugin.Marketplace.Dropship.Domains;

namespace Nop.Plugin.Marketplace.Dropship.Services
{
    public interface IDropshipFulfillmentService
    {
        Task InsertFulfillmentAsync(DropshipFulfillment fulfillment);

        // NEW METHODS:
        Task<DropshipFulfillment> GetByIdAsync(int id);
        Task UpdateFulfillmentAsync(DropshipFulfillment fulfillment);
        Task<IPagedList<DropshipFulfillment>> SearchSupplierTicketsAsync(int supplierVendorId, int pageIndex = 0, int pageSize = int.MaxValue);
    }
}