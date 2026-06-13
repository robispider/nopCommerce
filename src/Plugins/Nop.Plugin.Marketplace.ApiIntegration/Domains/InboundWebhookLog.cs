using System;
using Nop.Core;

namespace Nop.Plugin.Marketplace.ApiIntegration.Domains
{
    public class InboundWebhookLog : BaseEntity
    {
        public string ProviderSystemName { get; set; }
        public string EventId { get; set; } // The unique ID sent by the courier (e.g., FedEx Event ID)
        public string Payload { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}