using System;
using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Risk.Domains;

namespace Nop.Plugin.Marketplace.Risk.Data
{
    public class VendorReserveRuleBuilder : NopEntityBuilder<VendorReserveRule>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(VendorReserveRule.VendorId)).AsInt32().NotNullable().Indexed().Unique()
                .WithColumn(nameof(VendorReserveRule.HoldPercentage)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(VendorReserveRule.HoldDays)).AsInt32().NotNullable();
        }
    }

    public class ReserveScheduleBuilder : NopEntityBuilder<ReserveSchedule>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(ReserveSchedule.VendorId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(ReserveSchedule.EscrowTransactionId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(ReserveSchedule.HeldAmount)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(ReserveSchedule.ReleaseOnUtc)).AsDateTime2().NotNullable().Indexed()
                .WithColumn(nameof(ReserveSchedule.IsReleased)).AsBoolean().WithDefaultValue(false).Indexed()
                .WithColumn(nameof(ReserveSchedule.CreatedOnUtc)).AsDateTime2().NotNullable();
        }
    }

    public class ChargebackCaseBuilder : NopEntityBuilder<ChargebackCase>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(ChargebackCase.CoreOrderId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(ChargebackCase.VendorId)).AsInt32().NotNullable().Indexed()
                .WithColumn(nameof(ChargebackCase.DisputeAmount)).AsDecimal(18, 4).NotNullable()
                .WithColumn(nameof(ChargebackCase.Reason)).AsString(1000).NotNullable()
                .WithColumn(nameof(ChargebackCase.CreatedOnUtc)).AsDateTime2().NotNullable();
        }
    }

    [NopMigration("2026/01/01 03:00:00:0000000", "Marketplace.Risk base schema", MigrationProcessType.Installation)]
    public class RiskSchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<VendorReserveRule>();
            Create.TableFor<ReserveSchedule>();
            Create.TableFor<ChargebackCase>();

            // ALIBABA-GRADE: Seed the Global Fallback Reserve Rule (5% Hold for 14 Days)
            // Any vendor with no specific override rule will fallback to this to protect the platform.
            Insert.IntoTable(nameof(VendorReserveRule)).Row(new
            {
                VendorId = 0, // 0 denotes Global fallback
                HoldPercentage = 5.00m,
                HoldDays = 14
            });
        }
    }
}