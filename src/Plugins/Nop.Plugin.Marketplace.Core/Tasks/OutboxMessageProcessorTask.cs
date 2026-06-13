using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Data;
using Nop.Core.Events; // <-- Fixed namespace
using Nop.Services.ScheduleTasks; // <-- Fixed namespace
using Nop.Plugin.Marketplace.Core.Domains;

namespace Nop.Plugin.Marketplace.Core.Tasks
{
    public class OutboxMessageProcessorTask : IScheduleTask
    {
        private readonly IRepository<OutboxMessage> _outboxRepository;
        private readonly IEventPublisher _eventPublisher;

        public OutboxMessageProcessorTask(IRepository<OutboxMessage> outboxRepository, IEventPublisher eventPublisher)
        {
            _outboxRepository = outboxRepository;
            _eventPublisher = eventPublisher;
        }

        public async Task ExecuteAsync()
        {
            var pendingMessages = await _outboxRepository.Table
                .Where(x => !x.ProcessedOnUtc.HasValue && x.RetryCount < 5)
                .OrderBy(x => x.CreatedOnUtc).Take(50).ToListAsync();

            foreach (var message in pendingMessages)
            {
                try
                {
                    var eventType = Type.GetType($"{message.EventType}, Nop.Plugin.Marketplace.Core");
                    var eventInstance = JsonConvert.DeserializeObject(message.Payload, eventType);

                    var publishMethod = typeof(IEventPublisher)
                        .GetMethod(nameof(IEventPublisher.PublishAsync))
                        ?.MakeGenericMethod(eventType);

                    if (publishMethod != null)
                        await (Task)publishMethod.Invoke(_eventPublisher, new[] { eventInstance });

                    message.ProcessedOnUtc = DateTime.UtcNow;
                    await _outboxRepository.UpdateAsync(message);
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.Error = ex.Message;
                    await _outboxRepository.UpdateAsync(message);
                }
            }
        }
    }
}