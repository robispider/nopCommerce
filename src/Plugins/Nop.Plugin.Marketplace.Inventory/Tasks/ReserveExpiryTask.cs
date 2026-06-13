using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Plugin.Marketplace.Inventory.Domains;
using Nop.Plugin.Marketplace.Inventory.Domains.Enums;
using Nop.Plugin.Marketplace.Inventory.Services;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Marketplace.Inventory.Tasks
{
    /// <summary>
    /// Background task that automatically releases stock if a customer abandons checkout 
    /// or a payment fails to clear within the 15-minute TTL.
    /// </summary>
    public class ReserveExpiryTask : IScheduleTask
    {
        private readonly IRepository<StockReservation> _reservationRepository;
        private readonly IInventoryReservationService _reservationService;

        public ReserveExpiryTask(
            IRepository<StockReservation> reservationRepository,
            IInventoryReservationService reservationService)
        {
            _reservationRepository = reservationRepository;
            _reservationService = reservationService;
        }

        public async Task ExecuteAsync()
        {
            var now = DateTime.UtcNow;

            // Find all active reservations where the TTL has expired
            var expiredReservations = await _reservationRepository.GetAllAsync(q => q.Where(r =>
                r.StatusId == (int)StockReservationStatus.Active &&
                r.ExpiresOnUtc.HasValue &&
                r.ExpiresOnUtc.Value < now));

            foreach (var reservation in expiredReservations)
            {
                try
                {
                    // This safely returns the stock to the AvailableQuantity pool
                    await _reservationService.ReleaseReservationAsync(reservation.Id);
                }
                catch (Exception)
                {
                    // Log error but continue processing others
                    // Depending on logging setup, add ILogger here
                }
            }
        }
    }
}