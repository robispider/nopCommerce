using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Storefront.Services; // Add this using

namespace Nop.Plugin.Marketplace.Storefront.Infrastructure
{
    public class NopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register as Scoped: This ensures the Tier 1 cache (the private field) 
            // lives exactly for the duration of one HTTP request and is safely destroyed afterward.
            services.AddScoped<IStorefrontContext, StorefrontContext>();
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public int Order => 15;
    }
}