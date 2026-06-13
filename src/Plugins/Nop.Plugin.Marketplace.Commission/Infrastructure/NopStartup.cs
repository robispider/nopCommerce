using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Commission.Services;

namespace Nop.Plugin.Marketplace.Commission.Infrastructure
{
    public class NopStartup : INopStartup
    {
        public int Order => 3005; // Run after Core/Order

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICommissionEvaluatorService, CommissionEvaluatorService>();
        }

        public void Configure(IApplicationBuilder application) { }
    }
}