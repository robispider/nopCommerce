using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Risk.Consumers;
using Nop.Plugin.Marketplace.Risk.Tasks;

namespace Nop.Plugin.Marketplace.Risk.Infrastructure
{
    public class NopStartup : INopStartup
    {
        public int Order => 3010; // Execute after Wallet/Commission Startup

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register Consumers
            services.AddScoped<WalletSettledRiskConsumer>();

            // Register Cron Release Job
            services.AddScoped<ReserveReleaseTask>();
        }

        public void Configure(IApplicationBuilder application) { }
    }
}