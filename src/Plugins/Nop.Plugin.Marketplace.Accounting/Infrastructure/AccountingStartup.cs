using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Accounting.Consumers; // Add
using Nop.Plugin.Marketplace.Accounting.Services;
using Nop.Plugin.Marketplace.Accounting.Tasks;

namespace Nop.Plugin.Marketplace.Accounting.Infrastructure
{
    public class AccountingStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IAccountingService, AccountingService>();

            // Register Consumers
            services.AddScoped<OrderPaidAccountingConsumer>();
            services.AddScoped<SettlementAccountingConsumer>();
            services.AddScoped<ReserveHoldAccountingConsumer>();
            services.AddScoped<ReserveReleaseAccountingConsumer>();
            services.AddScoped<RefundAccountingConsumer>();
            services.AddScoped<WithdrawalAccountingConsumer>();
            // Register Audit Task
            services.AddScoped<AccountingReconciliationTask>();
        }

        public void Configure(IApplicationBuilder application) { }
        public int Order => 3000;
    }
}