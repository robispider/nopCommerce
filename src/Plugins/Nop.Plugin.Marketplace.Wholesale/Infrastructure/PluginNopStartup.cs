// Infrastructure/PluginNopStartup.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Wholesale.Services;

namespace Nop.Plugin.Marketplace.Wholesale.Infrastructure
{
    public class PluginNopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ISupplierProductService, SupplierProductService>();
        }

        public void Configure(IApplicationBuilder application) { }

        public int Order => 10;
    }
}