using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Data;
using Nop.Services.ScheduleTasks; // Fixed
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Plugin.Marketplace.Escrow.Domains;
using Nop.Plugin.Marketplace.Escrow.Services;

namespace Nop.Plugin.Marketplace.Escrow.Tasks
{
    public class EscrowAutoReleaseTask : IScheduleTask
    {
        private readonly IRepository<EscrowTransaction> _escrowRepository;
        private readonly IEscrowService _escrowService;

        public EscrowAutoReleaseTask(IRepository<EscrowTransaction> escrowRepository, IEscrowService escrowService)
        {
            _escrowRepository = escrowRepository;
            _escrowService = escrowService;
        }

        public async Task ExecuteAsync()
        {
            var threshold = DateTime.UtcNow.AddHours(-72);
            var expired = await _escrowRepository.Table
                .Where(e => e.CurrentStateId == (int)EscrowState.GracePeriod && e.UpdatedOnUtc <= threshold)
                .Take(100).ToListAsync();

            foreach (var escrow in expired)
            {
                try
                { await _escrowService.ReleaseFundsAsync(escrow.Id); }
                catch { continue; }
            }
        }
    }
}