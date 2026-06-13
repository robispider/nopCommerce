using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Escrow.Domains;
using Nop.Plugin.Marketplace.Order.Domains;

namespace Nop.Plugin.Marketplace.Escrow.Data
{
    public class EscrowTransactionBuilder : NopEntityBuilder<EscrowTransaction>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(EscrowTransaction.CoreOrderId)).AsInt32().Indexed().Unique()
                .WithColumn(nameof(EscrowTransaction.SupplierVendorId)).AsInt32().Indexed()
                .WithColumn(nameof(EscrowTransaction.ResellerVendorId)).AsInt32().Indexed()
                .WithColumn(nameof(EscrowTransaction.CurrentStateId)).AsInt32().Indexed()
                .WithColumn(nameof(EscrowTransaction.UpdatedOnUtc)).AsDateTime2()
            .WithColumn(nameof(MarketplaceOrderGroup.ConcurrencyVersion)).AsInt32().WithDefaultValue(1);
        }
    }
    public class EscrowStateHistoryBuilder : NopEntityBuilder<EscrowStateHistory>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(EscrowStateHistory.EscrowTransactionId)).AsInt32().Indexed()
                .WithColumn(nameof(EscrowStateHistory.OldStateId)).AsInt32()
                .WithColumn(nameof(EscrowStateHistory.NewStateId)).AsInt32()
                .WithColumn(nameof(EscrowStateHistory.SystemNote)).AsString(1000).Nullable()
                .WithColumn(nameof(EscrowStateHistory.AdminUserId)).AsInt32().Nullable()
                .WithColumn(nameof(EscrowStateHistory.CreatedOnUtc)).AsDateTime2();
        }
    }

    [NopMigration("2026/06/13 00:00:00:0000000", "Marketplace.Escrow base schema", MigrationProcessType.Installation)]
    public class EscrowSchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<EscrowTransaction>();
            Create.TableFor<EscrowStateHistory>();
        }
    }
}