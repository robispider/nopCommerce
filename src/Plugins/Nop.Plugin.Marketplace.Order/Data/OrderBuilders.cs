using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Order.Domains;

namespace Nop.Plugin.Marketplace.Order.Data
{
    public class MarketplaceOrderGroupBuilder : NopEntityBuilder<MarketplaceOrderGroup>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(MarketplaceOrderGroup.NativeOrderId)).AsInt32().NotNullable().Unique()
                .WithColumn(nameof(MarketplaceOrderGroup.TotalAmount)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(MarketplaceOrderGroup.StatusId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(MarketplaceOrderGroup.CreatedOnUtc)).AsDateTime2().NotNullable()
                .WithColumn(nameof(MarketplaceOrderGroup.UpdatedOnUtc)).AsDateTime2().NotNullable()
                .WithColumn(nameof(MarketplaceOrderGroup.CompletedOnUtc)).AsDateTime2().Nullable();
        }
    }

    public class MarketplaceOrderAllocationBuilder : NopEntityBuilder<MarketplaceOrderAllocation>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(MarketplaceOrderAllocation.MarketplaceOrderGroupId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(MarketplaceOrderAllocation.VendorId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(MarketplaceOrderAllocation.OrderItemId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(MarketplaceOrderAllocation.AllocatedAmount)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(MarketplaceOrderAllocation.FulfillmentMethodId)).AsInt32().NotNullable()
                .WithColumn(nameof(MarketplaceOrderAllocation.StatusId)).AsInt32().NotNullable()
                .WithColumn(nameof(MarketplaceOrderAllocation.CreatedOnUtc)).AsDateTime2().NotNullable()
                .WithColumn(nameof(MarketplaceOrderAllocation.UpdatedOnUtc)).AsDateTime2().NotNullable();
        }
    }
}