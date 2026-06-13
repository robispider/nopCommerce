using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Inventory.Consumers;
using Nop.Plugin.Marketplace.Inventory.Services;
using Nop.Plugin.Marketplace.Inventory.Tasks;

namespace Nop.Plugin.Marketplace.Inventory.Infrastructure
{
    public class NopStartup : INopStartup
    {
        public int Order => 3000;

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Sprint 1 Services
            services.AddScoped<IInventoryBucketService, InventoryBucketService>();
            services.AddScoped<SupplierStockChangedEventConsumer>();

            // Sprint 2 Services
            services.AddScoped<IAllocationRuleService, AllocationRuleService>();
            services.AddScoped<IInventoryReservationService, InventoryReservationService>();

            // Register Background Tasks
            services.AddScoped<ReserveExpiryTask>();
        }

        public void Configure(IApplicationBuilder application)
        {
        }
    }
}