using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Nop.Plugin.Marketplace.ApiIntegration.Services
{
    public class CourierWebhookResult
    {
        public bool IsValid { get; set; }
        public string EventId { get; set; }
        public string TrackingNumber { get; set; }
        public bool IsDelivered { get; set; }
        public string ErrorMessage { get; set; }
    }

    public interface ICourierProvider
    {
        string SystemName { get; }

        // Verifies the HMAC/Signature from the Request Headers
        Task<bool> VerifySignatureAsync(HttpRequest request, string rawPayload);

        // Parses the JSON into our standard internal model
        CourierWebhookResult ParsePayload(string rawPayload);
    }
}