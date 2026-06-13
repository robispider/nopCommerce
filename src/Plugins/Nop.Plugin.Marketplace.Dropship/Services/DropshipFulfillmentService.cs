using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Events; // Required for ShipmentDeliveredEvent
using Nop.Plugin.Marketplace.Dropship.Domains;
using Nop.Services.Events; // Required for IEventPublisher

namespace Nop.Plugin.Marketplace.Dropship.Services
{
    public class DropshipFulfillmentService : IDropshipFulfillmentService
    {
        private readonly IRepository<DropshipFulfillment> _fulfillmentRepository;
        private readonly IEventPublisher _eventPublisher; // Added

        public DropshipFulfillmentService(
            IRepository<DropshipFulfillment> fulfillmentRepository,
            IEventPublisher eventPublisher) // Injected
        {
            _fulfillmentRepository = fulfillmentRepository;
            _eventPublisher = eventPublisher;
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

            var totalCount = query.Count();
            var records = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();

            return await Task.FromResult(new PagedList<DropshipFulfillment>(records, pageIndex, pageSize, totalCount));
        }

        public async Task<DropshipFulfillment> GetByTrackingNumberAsync(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                return null;
            var query = _fulfillmentRepository.Table.Where(f => f.TrackingNumber == trackingNumber);
            return await Task.FromResult(query.FirstOrDefault());
        }

        public async Task<IList<DropshipFulfillment>> GetTicketsByOrderIdAsync(int orderId)
        {
            var query = _fulfillmentRepository.Table.Where(f => f.OrderId == orderId);
            return await Task.FromResult(query.ToList());
        }

        public async Task MarkAsDeliveredAsync(string trackingNumber, string courierSystemName)
        {
            var query = _fulfillmentRepository.Table.Where(f =>
                f.TrackingNumber == trackingNumber &&
                f.CourierSystemName == courierSystemName);

            var ticket = await Task.FromResult(query.FirstOrDefault());

            if (ticket == null || ticket.DropshipStatusId >= (int)DropshipStatus.Delivered)
                return;

            // 1. Update State
            ticket.DropshipStatusId = (int)DropshipStatus.Delivered;
            ticket.DeliveredOnUtc = DateTime.UtcNow;
            await _fulfillmentRepository.UpdateAsync(ticket);

            // 2. Publish System Event
            await _eventPublisher.PublishAsync(new ShipmentDeliveredEvent
            {
                DropshipFulfillmentId = ticket.Id,
                NativeOrderId = ticket.OrderId, // Fixed property name
                TrackingNumber = trackingNumber,
                CourierSystemName = courierSystemName,
                DeliveredOnUtc = ticket.DeliveredOnUtc.Value
            });
        }
        public async Task MarkAsReturnedToOriginAsync(string trackingNumber, string courierSystemName, string reason)
        {
            var query = _fulfillmentRepository.Table.Where(f =>
                f.TrackingNumber == trackingNumber &&
                f.CourierSystemName == courierSystemName);

            var ticket = await Task.FromResult(query.FirstOrDefault());

            // Idempotency: Ignore if already RTO or Delivered
            if (ticket == null || ticket.DropshipStatusId == (int)DropshipStatus.Delivered || ticket.DropshipStatusId == (int)DropshipStatus.ReturnToOrigin)
                return;

            // 1. Update State
            ticket.DropshipStatusId = (int)DropshipStatus.ReturnToOrigin;
            await _fulfillmentRepository.UpdateAsync(ticket);

            // 2. Publish System Event
            await _eventPublisher.PublishAsync(new ShipmentReturnToOriginEvent
            {
                DropshipFulfillmentId = ticket.Id,
                NativeOrderId = ticket.OrderId,
                TrackingNumber = trackingNumber,
                Reason = reason,
                ReturnedOnUtc = DateTime.UtcNow
            });
        }
    }
}