using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Accounting.Services;

namespace Nop.Plugin.Marketplace.Accounting.Infrastructure
{
    public class AccountingStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IAccountingService, AccountingService>();
        }

        public void Configure(IApplicationBuilder application) { }
        public int Order => 3000;
    }
}