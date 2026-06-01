using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Manual.Models;

public record CreditCardInfoModel: BaseNopModel
{
    public int OrderId { get; set; }

    [NopResourceDisplayName("Admin.Orders.Fields.CardType")]
    public string CardType { get; set; }
    [NopResourceDisplayName("Admin.Orders.Fields.CardName")]
    public string CardName { get; set; }
    [NopResourceDisplayName("Admin.Orders.Fields.CardNumber")]
    public string CardNumber { get; set; }
    [NopResourceDisplayName("Admin.Orders.Fields.CardCVV2")]
    public string CardCvv2 { get; set; }
    [NopResourceDisplayName("Admin.Orders.Fields.CardExpirationMonth")]
    public string CardExpirationMonth { get; set; }
    [NopResourceDisplayName("Admin.Orders.Fields.CardExpirationYear")]
    public string CardExpirationYear { get; set; }
}
