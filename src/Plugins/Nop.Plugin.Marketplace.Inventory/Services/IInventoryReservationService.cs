using System.Threading.Tasks;
using Nop.Plugin.Marketplace.Inventory.Domains;

namespace Nop.Plugin.Marketplace.Inventory.Services
{
    public interface IInventoryReservationService
    {
        Task<StockReservation> ReserveStockAsync(int productId, int? resellerVendorId, int orderItemId, int quantity, int expiryMinutes = 15);
        Task ConfirmReservationAsync(int reservationId);
        Task ReleaseReservationAsync(int reservationId);
    }
}