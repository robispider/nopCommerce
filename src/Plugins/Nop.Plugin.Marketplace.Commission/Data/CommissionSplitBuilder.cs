using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Commission.Domains;

namespace Nop.Plugin.Marketplace.Commission.Data
{
    public class CommissionSplitBuilder : NopEntityBuilder<CommissionSplit>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(CommissionSplit.NativeOrderId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(CommissionSplit.OrderItemId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(CommissionSplit.AppliedBaseRuleId)).AsInt32().NotNullable()
                .WithColumn(nameof(CommissionSplit.AppliedModifierRuleIds)).AsString(200).Nullable()
                .WithColumn(nameof(CommissionSplit.CustomerPaidAmount)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(CommissionSplit.TaxAmount)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(CommissionSplit.GatewayFeeAmount)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(CommissionSplit.PlatformFeeAmount)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(CommissionSplit.SupplierVendorId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(CommissionSplit.SupplierWholesaleAmount)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(CommissionSplit.ResellerVendorId)).AsInt32().Nullable().Indexed()
                .WithColumn(nameof(CommissionSplit.ResellerMarginAmount)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(CommissionSplit.CreatedOnUtc)).AsDateTime2().NotNullable();
        }
    }
}