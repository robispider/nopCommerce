using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Data;
using Nop.Plugin.Marketplace.ApiIntegration.Domains;
using Nop.Plugin.Marketplace.ApiIntegration.Services;
using Nop.Plugin.Marketplace.Dropship.Services;

namespace Nop.Plugin.Marketplace.ApiIntegration.Controllers
{
    [Route("api/market/webhooks/courier")]
    [ApiController]
    public class CourierWebhookController : ControllerBase
    {
        private readonly ICourierProviderFactory _providerFactory;
        private readonly IDropshipFulfillmentService _dropshipService;
        private readonly IRepository<InboundWebhookLog> _webhookLogRepository;

        public CourierWebhookController(
            ICourierProviderFactory providerFactory,
            IDropshipFulfillmentService dropshipService,
            IRepository<InboundWebhookLog> webhookLogRepository)
        {
            _providerFactory = providerFactory;
            _dropshipService = dropshipService;
            _webhookLogRepository = webhookLogRepository;
        }

        [HttpPost("{providerSystemName}")]
        public async Task<IActionResult> HandleWebhook(string providerSystemName)
        {
            // 1. Resolve Courier Strategy
            var provider = _providerFactory.GetProvider(providerSystemName);
            if (provider == null)
                return NotFound($"Courier provider '{providerSystemName}' not registered.");

            // 2. Read Raw Payload
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body);
            var rawPayload = await reader.ReadToEndAsync();

            // 3. SECURE VERIFICATION (HMAC / Signatures)
            if (!await provider.VerifySignatureAsync(Request, rawPayload))
                return Unauthorized("Invalid webhook signature.");

            // 4. Parse Payload
            var result = provider.ParsePayload(rawPayload);
            if (!result.IsValid)
                return BadRequest(result.ErrorMessage);

            // 5. TRUE IDEMPOTENCY CHECK (Database Level)
            var existingLog = await _webhookLogRepository.Table
                .FirstOrDefaultAsync(x => x.ProviderSystemName == provider.SystemName && x.EventId == result.EventId);

            if (existingLog != null)
                return Ok(new { message = "Already processed" }); // Prevent Double-Processing!

            try
            {
                // 6. Business Logic: If Delivered, mark it.
                if (result.IsDelivered)
                {
                    await _dropshipService.MarkAsDeliveredAsync(result.TrackingNumber, provider.SystemName);
                }

                // 7. Save Webhook Log to finalize Idempotency
                await _webhookLogRepository.InsertAsync(new InboundWebhookLog
                {
                    ProviderSystemName = provider.SystemName,
                    EventId = result.EventId,
                    Payload = rawPayload,
                    CreatedOnUtc = DateTime.UtcNow
                });

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                // In production, log `ex.Message` to Nop ILogger
                return StatusCode(500, "Internal processing error.");
            }
        }
    }
}