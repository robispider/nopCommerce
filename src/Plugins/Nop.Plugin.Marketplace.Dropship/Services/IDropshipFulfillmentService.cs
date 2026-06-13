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

        Task<DropshipFulfillment> GetByTrackingNumberAsync(string trackingNumber);
        Task<IList<DropshipFulfillment>> GetTicketsByOrderIdAsync(int orderId);

        /// <summary>
        /// Safely transitions a ticket to Delivered and fires the system event.
        /// </summary>
        Task MarkAsDeliveredAsync(string trackingNumber, string courierSystemName);
        Task MarkAsReturnedToOriginAsync(string trackingNumber, string courierSystemName, string reason);
    }
}