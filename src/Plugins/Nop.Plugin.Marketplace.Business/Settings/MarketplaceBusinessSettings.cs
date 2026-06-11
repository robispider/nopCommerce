using System.Runtime;
using Nop.Core.Configuration;

namespace Nop.Plugin.Marketplace.Business.Settings
{
    public class MarketplaceBusinessSettings : ISettings
    {
        public string MinioEndpoint { get; set; } = "localhost:9000";
        public string MinioAccessKey { get; set; } = "minioadmin";
        public string MinioSecretKey { get; set; } = "minioadminpassword";
        public string KycBucketName { get; set; } = "marketplace-kyc-docs";
        public bool UseSSL { get; set; } = false;
    }
}