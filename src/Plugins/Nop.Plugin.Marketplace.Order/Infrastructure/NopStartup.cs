using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Order.Consumers;
using Nop.Plugin.Marketplace.Order.Services;

namespace Nop.Plugin.Marketplace.Order.Infrastructure
{
    public class NopStartup : INopStartup
    {
        public int Order => 3001; // Runs after Inventory

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IOrderAllocationService, OrderAllocationService>();
            services.AddScoped<OrderPlacedEventConsumer>();
        }

        public void Configure(IApplicationBuilder application)
        {
        }
    }
}