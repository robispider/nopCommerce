using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Dropship.Services;

namespace Nop.Plugin.Marketplace.Dropship.Infrastructure
{
    public class PluginNopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register our local dropship service
            services.AddScoped<IDropshipFulfillmentService, DropshipFulfillmentService>();
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public int Order => 10;
    }
}