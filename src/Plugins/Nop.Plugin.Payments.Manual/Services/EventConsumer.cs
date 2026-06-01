using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Nop.Core.Domain.Security;
using Nop.Plugin.Payments.Manual.Models;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Manual.Services;

/// <summary>
/// Represents plugin event consumer
/// </summary>
public class EventConsumer : IConsumer<SecuritySettingsChangedEvent>
{
    #region Fields

    private readonly IEncryptionService _encryptionService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    #endregion

    #region Ctor

    public EventConsumer(IEncryptionService encryptionService, IGenericAttributeService genericAttributeService, IOrderService orderService, IPaymentService paymentService, IHttpContextAccessor httpContextAccessor)
    {
        _encryptionService = encryptionService;
        _genericAttributeService = genericAttributeService;
        _orderService = orderService;
        _paymentService = paymentService;
        _httpContextAccessor = httpContextAccessor;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Handle event
    /// </summary>
    /// <param name="eventMessage">Event</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task HandleEventAsync(SecuritySettingsChangedEvent eventMessage)
    {
        if (string.Equals(eventMessage.OldEncryptionPrivateKey, eventMessage.SecuritySettings.EncryptionKey))
            return;

        var orders = await _orderService.SearchOrdersAsync();
        
        foreach (var order in orders)
        {
            var json = await _genericAttributeService.GetAttributeAsync<string>(order, nameof(CreditCardInfo));

            if (string.IsNullOrEmpty(json))
                continue;

            var creditCardInfo = JsonConvert.DeserializeObject<CreditCardInfo>(json);

            var decryptedCardType = _encryptionService.DecryptText(creditCardInfo.CardType, eventMessage.OldEncryptionPrivateKey);
            var decryptedCardName = _encryptionService.DecryptText(creditCardInfo.CardName, eventMessage.OldEncryptionPrivateKey);
            var decryptedCardNumber = _encryptionService.DecryptText(creditCardInfo.CardNumber, eventMessage.OldEncryptionPrivateKey);
            var decryptedMaskedCreditCardNumber = _encryptionService.DecryptText(creditCardInfo.MaskedCreditCardNumber, eventMessage.OldEncryptionPrivateKey);
            var decryptedCardCvv2 = _encryptionService.DecryptText(creditCardInfo.CardCvv2, eventMessage.OldEncryptionPrivateKey);
            var decryptedCardExpirationMonth = _encryptionService.DecryptText(creditCardInfo.CardExpirationMonth, eventMessage.OldEncryptionPrivateKey);
            var decryptedCardExpirationYear = _encryptionService.DecryptText(creditCardInfo.CardExpirationYear, eventMessage.OldEncryptionPrivateKey);

            var encryptedCardType = _encryptionService.EncryptText(decryptedCardType, eventMessage.SecuritySettings.EncryptionKey);
            var encryptedCardName = _encryptionService.EncryptText(decryptedCardName, eventMessage.SecuritySettings.EncryptionKey);
            var encryptedCardNumber = _encryptionService.EncryptText(decryptedCardNumber, eventMessage.SecuritySettings.EncryptionKey);
            var encryptedMaskedCreditCardNumber = _encryptionService.EncryptText(decryptedMaskedCreditCardNumber, eventMessage.SecuritySettings.EncryptionKey);
            var encryptedCardCvv2 = _encryptionService.EncryptText(decryptedCardCvv2, eventMessage.SecuritySettings.EncryptionKey);
            var encryptedCardExpirationMonth = _encryptionService.EncryptText(decryptedCardExpirationMonth, eventMessage.SecuritySettings.EncryptionKey);
            var encryptedCardExpirationYear = _encryptionService.EncryptText(decryptedCardExpirationYear, eventMessage.SecuritySettings.EncryptionKey);

            creditCardInfo.CardType = encryptedCardType;
            creditCardInfo.CardName = encryptedCardName;
            creditCardInfo.CardNumber = encryptedCardNumber;
            creditCardInfo.MaskedCreditCardNumber = encryptedMaskedCreditCardNumber;
            creditCardInfo.CardCvv2 = encryptedCardCvv2;
            creditCardInfo.CardExpirationMonth = encryptedCardExpirationMonth;
            creditCardInfo.CardExpirationYear = encryptedCardExpirationYear;

            json = JsonConvert.SerializeObject(creditCardInfo);
            await _genericAttributeService.SaveAttributeAsync(order, nameof(CreditCardInfo), json);
        }
    }

    #endregion
}