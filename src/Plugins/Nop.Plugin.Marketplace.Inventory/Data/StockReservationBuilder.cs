using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Inventory.Domains;

namespace Nop.Plugin.Marketplace.Inventory.Data
{
    public class StockReservationBuilder : NopEntityBuilder<StockReservation>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(StockReservation.InventoryBucketId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(StockReservation.OrderItemId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(StockReservation.QuantityReserved)).AsInt32().NotNullable()
                .WithColumn(nameof(StockReservation.ExpiresOnUtc)).AsDateTime2().Nullable().Indexed()
                .WithColumn(nameof(StockReservation.StatusId)).AsInt32().NotNullable()
                .WithColumn(nameof(StockReservation.CreatedOnUtc)).AsDateTime2().NotNullable()
                .WithColumn(nameof(StockReservation.ReleasedOnUtc)).AsDateTime2().Nullable();
        }
    }
}