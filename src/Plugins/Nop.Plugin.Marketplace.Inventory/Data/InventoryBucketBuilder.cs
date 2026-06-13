using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Inventory.Domains;

namespace Nop.Plugin.Marketplace.Inventory.Data
{
    public class InventoryBucketBuilder : NopEntityBuilder<InventoryBucket>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(InventoryBucket.ProductId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(InventoryBucket.SourceVendorId)).AsInt32().Nullable().Indexed()
                .WithColumn(nameof(InventoryBucket.BucketTypeId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(InventoryBucket.AvailableQuantity)).AsInt32().NotNullable()
                .WithColumn(nameof(InventoryBucket.ReservedQuantity)).AsInt32().NotNullable()
                .WithColumn(nameof(InventoryBucket.BackorderQuantity)).AsInt32().NotNullable()
                .WithColumn(nameof(InventoryBucket.ConcurrencyVersion)).AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn(nameof(InventoryBucket.UpdatedOnUtc)).AsDateTime2().NotNullable();
        }
    }
}