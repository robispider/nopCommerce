using System.Threading.Tasks;
using Nop.Core.Domain.ScheduleTasks;

using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;

using System.Threading.Tasks;
namespace Nop.Plugin.Marketplace.Escrow
{
    public class MarketplaceEscrowPlugin : BasePlugin
    {
        private readonly IScheduleTaskService _scheduleTaskService;

        public MarketplaceEscrowPlugin(IScheduleTaskService scheduleTaskService)
        {
            _scheduleTaskService = scheduleTaskService;
        }

        public override async Task InstallAsync()
        {
            // Register Auto-Release to run every 1 hour (3600 seconds)
            var taskType = "Nop.Plugin.Marketplace.Escrow.Tasks.EscrowAutoReleaseTask, Nop.Plugin.Marketplace.Escrow";
            var task = await _scheduleTaskService.GetTaskByTypeAsync(taskType);

            if (task == null)
            {
                await _scheduleTaskService.InsertTaskAsync(new ScheduleTask
                {
                    Enabled = true,
                    Seconds = 3600,
                    Name = "Marketplace Escrow Auto-Release",
                    Type = taskType,
                    StopOnError = false
                });
            }

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            var taskType = "Nop.Plugin.Marketplace.Escrow.Tasks.EscrowAutoReleaseTask, Nop.Plugin.Marketplace.Escrow";
            var task = await _scheduleTaskService.GetTaskByTypeAsync(taskType);
            if (task != null)
                await _scheduleTaskService.DeleteTaskAsync(task);

            await base.UninstallAsync();
        }
    }
}