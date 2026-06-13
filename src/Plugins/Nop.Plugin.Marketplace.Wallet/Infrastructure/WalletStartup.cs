using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Marketplace.Wallet.Services;
namespace Nop.Plugin.Marketplace.Wallet.Infrastructure
{
    public class WalletStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register our ACID-compliant Ledger Service
            services.AddScoped<IWalletTransactionService, WalletTransactionService>();
        }

        public void Configure(IApplicationBuilder application) { }

        // Ensure this runs after core nopCommerce services
        public int Order => 3000;
    }
}