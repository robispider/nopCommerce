using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Caching; // Inject locker
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Marketplace.Core.Events;
using Nop.Plugin.Marketplace.Risk.Domains;
using Nop.Services.Events;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Marketplace.Risk.Tasks
{
    public class ReserveReleaseTask : IScheduleTask
    {
        private readonly IRepository<ReserveSchedule> _scheduleRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILocker _locker;

        public ReserveReleaseTask(
            IRepository<ReserveSchedule> scheduleRepository,
            IEventPublisher eventPublisher,
            ILocker locker)
        {
            _scheduleRepository = scheduleRepository;
            _eventPublisher = eventPublisher;
            _locker = locker;
        }

        public async Task ExecuteAsync()
        {
            var maturedSchedules = await _scheduleRepository.Table
                .Where(x => !x.IsReleased && x.ReleaseOnUtc <= DateTime.UtcNow)
                .Take(100).ToListAsync();

            foreach (var schedule in maturedSchedules)
            {
                string lockKey = $"marketplace_reserve_release_{schedule.Id}";

                // ALIBABA-GRADE: Lock each schedule row so two concurrent release tasks never double-credit
                await _locker.PerformActionWithLockAsync(lockKey, TimeSpan.FromSeconds(15), async () =>
                {
                    // Reload record inside the lock to verify status
                    var freshSchedule = await _scheduleRepository.GetByIdAsync(schedule.Id);
                    if (freshSchedule == null || freshSchedule.IsReleased)
                        return;

                    freshSchedule.IsReleased = true;
                    await _scheduleRepository.UpdateAsync(freshSchedule);

                    // Publish the release event so the Wallet can credit available balances instantly
                    await _eventPublisher.PublishAsync(new ReserveReleasedEvent
                    {
                        VendorId = freshSchedule.VendorId,
                        Amount = freshSchedule.HeldAmount,
                        ReserveScheduleId = freshSchedule.Id,
                        IdempotencyKey = $"RSV_REL_{freshSchedule.Id}"
                    });
                });
            }
        }
    }
}