using Nop.Core.Configuration;

namespace Nop.Plugin.Marketplace.Commission.Settings
{
    public class MarketplaceCommissionSettings : ISettings
    {
        public decimal GatewayFeePercentage { get; set; } = 2.9m; // e.g. 2.9%
        public decimal GatewayFeeFixed { get; set; } = 0.30m;      // e.g. $0.30
    }
}