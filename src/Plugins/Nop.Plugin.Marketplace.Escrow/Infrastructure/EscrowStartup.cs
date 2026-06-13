using Humanizer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Escrow.Services;

namespace Nop.Plugin.Marketplace.Escrow.Infrastructure
{
    public class EscrowStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register Escrow Service
            services.AddScoped<IEscrowService, EscrowService>();
            
services.AddScoped<ICommissionService, CommissionService>();
        }

        public void Configure(IApplicationBuilder application) { }

        public int Order => 3000;
    }
}