using Nop.Core;

namespace Nop.Plugin.Marketplace.Core.Domains
{
    public partial class OutboxMessage : BaseEntity
    {
        public string EventType { get; set; } // e.g., "EscrowReleasedEvent"
        public string Payload { get; set; }   // JSON serialized event data
        public DateTime CreatedOnUtc { get; set; }
        public DateTime? ProcessedOnUtc { get; set; }
        public string Error { get; set; }
        public int RetryCount { get; set; }
    }
}