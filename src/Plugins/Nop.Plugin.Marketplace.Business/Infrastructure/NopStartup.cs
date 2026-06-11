using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Business.Services;

namespace Nop.Plugin.Marketplace.Business.Infrastructure
{
    /// <summary>
    /// Represents the startup configuration for the plugin.
    /// This replaces the deprecated IDependencyRegistrar.
    /// </summary>
    public class NopStartup : INopStartup
    {
        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register your custom services here
            services.AddScoped<IMarketplaceDocumentService, MarketplaceDocumentService>();
            services.AddScoped<IMarketplaceBusinessService, MarketplaceBusinessService>();
            
            services.AddScoped<IMarketplaceCatalogService, MarketplaceCatalogService>();
        }

        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        public void Configure(IApplicationBuilder application)
        {
            // We don't need to add any custom HTTP middleware pipeline steps for this plugin yet
        }

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        public int Order => 10;
    }
}