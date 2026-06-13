using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Risk.Domains;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Marketplace.Risk.Tasks
{
    public class ReserveReleaseTask : IScheduleTask
    {
        private readonly IRepository<ReserveSchedule> _scheduleRepository;
        private readonly IRepository<OutboxMessage> _outboxRepository;

        public ReserveReleaseTask(IRepository<ReserveSchedule> scheduleRepository, IRepository<OutboxMessage> outboxRepository)
        {
            _scheduleRepository = scheduleRepository;
            _outboxRepository = outboxRepository;
        }

        public async Task ExecuteAsync()
        {
            var maturedSchedules = await _scheduleRepository.Table
                .Where(x => !x.IsReleased && x.ReleaseOnUtc <= DateTime.UtcNow)
                .Take(100).ToListAsync();

            foreach (var schedule in maturedSchedules)
            {
                schedule.IsReleased = true;
                await _scheduleRepository.UpdateAsync(schedule);

                var releaseEvent = new ReserveReleasedEvent
                {
                    VendorId = schedule.VendorId,
                    Amount = schedule.HeldAmount,
                    ReserveScheduleId = schedule.Id,
                    IdempotencyKey = $"RSV_REL_{schedule.Id}"
                };

                await _outboxRepository.InsertAsync(new OutboxMessage
                {
                    EventType = "Nop.Plugin.Marketplace.Core.Events.ReserveReleasedEvent",
                    Payload = JsonConvert.SerializeObject(releaseEvent),
                    CreatedOnUtc = DateTime.UtcNow
                });
            }
        }
    }
}