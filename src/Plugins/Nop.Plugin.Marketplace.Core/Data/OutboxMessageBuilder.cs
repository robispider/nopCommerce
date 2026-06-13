using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Core.Domains;

namespace Nop.Plugin.Marketplace.Core.Data
{
    public class OutboxMessageBuilder : NopEntityBuilder<OutboxMessage>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(OutboxMessage.EventType)).AsString(400).NotNullable()
                .WithColumn(nameof(OutboxMessage.Payload)).AsString(int.MaxValue).NotNullable()
                .WithColumn(nameof(OutboxMessage.CreatedOnUtc)).AsDateTime2().NotNullable()
                .WithColumn(nameof(OutboxMessage.ProcessedOnUtc)).AsDateTime2().Nullable()
                .WithColumn(nameof(OutboxMessage.Error)).AsString(1000).Nullable()
                .WithColumn(nameof(OutboxMessage.RetryCount)).AsInt32().WithDefaultValue(0);
        }
    }
}