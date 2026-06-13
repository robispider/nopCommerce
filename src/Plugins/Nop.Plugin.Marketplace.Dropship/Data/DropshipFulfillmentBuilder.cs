using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Dropship.Domains;

namespace Nop.Plugin.Marketplace.Dropship.Data
{
    public class DropshipFulfillmentBuilder : NopEntityBuilder<DropshipFulfillment>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(DropshipFulfillment.OrderId)).AsInt32().Indexed()
                .WithColumn(nameof(DropshipFulfillment.OrderItemId)).AsInt32().Indexed()
                .WithColumn(nameof(DropshipFulfillment.ResellerVendorId)).AsInt32().Indexed()
                .WithColumn(nameof(DropshipFulfillment.SupplierVendorId)).AsInt32().Indexed()

                .WithColumn(nameof(DropshipFulfillment.LockedWholesalePrice)).AsDecimal(18, 4)
                .WithColumn(nameof(DropshipFulfillment.LockedRetailPrice)).AsDecimal(18, 4)

                .WithColumn(nameof(DropshipFulfillment.DropshipStatusId)).AsInt32()
                .WithColumn(nameof(DropshipFulfillment.TrackingNumber)).AsString(255).Nullable()

                .WithColumn(nameof(DropshipFulfillment.CreatedOnUtc)).AsDateTime2()
                .WithColumn(nameof(DropshipFulfillment.ShippedOnUtc)).AsDateTime2().Nullable();
        }
    }
}