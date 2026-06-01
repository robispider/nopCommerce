using FluentMigrator;
using Newtonsoft.Json;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Data.Extensions;

namespace Nop.Data.Migrations.UpgradeTo500;

[NopMigration("2026-05-27 00:00:03", "Move \"credit card info\" fields into generic attributes", MigrationProcessType.Update)]
public class OrderMigration : ForwardOnlyMigration
{
    private readonly INopDataProvider _dataProvider;

    public OrderMigration(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        //#8169
        if (!Schema.Table(nameof(Order)).Column("AllowStoringCreditCardNumber").Exists())
            return;

        var dataSettings = DataSettingsManager.LoadSettings();

        var tableName = dataSettings.DataProvider switch
        {
            DataProviderType.SqlServer => $"[{nameof(Order)}]",
            DataProviderType.MySql => $"`{nameof(Order)}`",
            DataProviderType.PostgreSQL => $"\"{nameof(Order)}\"",
            _ => throw new NotSupportedException("This data provider is not supported")
        };

        var orders = _dataProvider.Query<dynamic>($"SELECT * FROM {tableName} WHERE AllowStoringCreditCardNumber = 1");

        if (!orders.Any())
            return;

        var attributes = orders.Select(order => new GenericAttribute
        {
            EntityId = order.Id,
            KeyGroup = nameof(Order),
            Key = "CreditCardInfo",
            Value = JsonConvert.SerializeObject(new
            {
                CardType = order.CardType?.ToString(),
                CardName = order.CardName?.ToString(),
                CardNumber = order.CardNumber?.ToString(),
                MaskedCreditCardNumber = order.MaskedCreditCardNumber?.ToString(),
                CardCvv2 = order.CardCvv2?.ToString(),
                CardExpirationMonth = order.CardExpirationMonth?.ToString(),
                CardExpirationYear = order.CardExpirationYear?.ToString()
            }),
            StoreId = order.StoreId,
            CreatedOrUpdatedDateUTC = DateTime.UtcNow
        });

        _dataProvider.BulkInsertEntities(attributes);

        this.DeleteColumnsIfExists<Order>(["AllowStoringCreditCardNumber", "CardType", "CardName", "CardNumber", "MaskedCreditCardNumber", "CardCvv2", "CardExpirationMonth", "CardExpirationYear"]);
    }
}
