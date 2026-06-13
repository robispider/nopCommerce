using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Commission.Domains;

namespace Nop.Plugin.Marketplace.Commission.Data
{
    public class CommissionRuleBuilder : NopEntityBuilder<CommissionRule>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(CommissionRule.Name)).AsString(200).NotNullable()
                .WithColumn(nameof(CommissionRule.PriorityId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(CommissionRule.IsModifier)).AsBoolean().NotNullable()
                .WithColumn(nameof(CommissionRule.CalculationTypeId)).AsInt32().NotNullable()
                .WithColumn(nameof(CommissionRule.TargetVendorId)).AsInt32().Nullable().Indexed()
                .WithColumn(nameof(CommissionRule.TargetProductId)).AsInt32().Nullable().Indexed()
                .WithColumn(nameof(CommissionRule.TargetCategoryId)).AsInt32().Nullable().Indexed()
                .WithColumn(nameof(CommissionRule.Percentage)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(CommissionRule.FixedAmount)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(CommissionRule.MinimumFeeAmount)).AsDecimal(18, 4).Nullable()
                .WithColumn(nameof(CommissionRule.MaximumFeeAmount)).AsDecimal(18, 4).Nullable()
                .WithColumn(nameof(CommissionRule.EffectiveFromUtc)).AsDateTime2().Nullable().Indexed()
                .WithColumn(nameof(CommissionRule.EffectiveToUtc)).AsDateTime2().Nullable().Indexed()
                .WithColumn(nameof(CommissionRule.IsActive)).AsBoolean().WithDefaultValue(true)
                .WithColumn(nameof(CommissionRule.CreatedOnUtc)).AsDateTime2().NotNullable();
        }
    }
}