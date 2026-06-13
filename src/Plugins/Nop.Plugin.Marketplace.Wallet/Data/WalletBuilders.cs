using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Wallet.Domains;

namespace Nop.Plugin.Marketplace.Wallet.Data
{
    public class WalletAccountBuilder : NopEntityBuilder<WalletAccount>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(WalletAccount.VendorId)).AsInt32().Indexed().Unique()
                .WithColumn(nameof(WalletAccount.AvailableBalance)).AsDecimal(18, 4).WithDefaultValue(0)
                .WithColumn(nameof(WalletAccount.PendingBalance)).AsDecimal(18, 4).WithDefaultValue(0)
                .WithColumn(nameof(WalletAccount.ReserveBalance)).AsDecimal(18, 4).WithDefaultValue(0)
                .WithColumn(nameof(WalletAccount.ConcurrencyVersion)).AsInt32().WithDefaultValue(1);
        }
    }

    public class WalletLedgerBuilder : NopEntityBuilder<WalletLedger>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(WalletLedger.WalletAccountId)).AsInt32().Indexed()
                .WithColumn(nameof(WalletLedger.EntryTypeId)).AsInt32()
                .WithColumn(nameof(WalletLedger.Amount)).AsDecimal(18, 4)
                .WithColumn(nameof(WalletLedger.ReferenceType)).AsString(100).Nullable()
                .WithColumn(nameof(WalletLedger.ReferenceId)).AsInt32()
                .WithColumn(nameof(WalletLedger.IdempotencyKey)).AsString(255).Unique() // CRITICAL
                .WithColumn(nameof(WalletLedger.Notes)).AsString(1000).Nullable()
                .WithColumn(nameof(WalletLedger.CreatedOnUtc)).AsDateTime2();
        }
    }

    [NopMigration("2026/06/13 00:00:00:0000000", "Marketplace.Wallet base schema", MigrationProcessType.Installation)]
    public class WalletSchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<WalletAccount>();
            Create.TableFor<WalletLedger>();
            Create.TableFor<WithdrawalRequest>();
        }
    }

    // ADD THIS NEW BUILDER:
    public class WithdrawalRequestBuilder : NopEntityBuilder<WithdrawalRequest>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(WithdrawalRequest.VendorId)).AsInt32().Indexed()
                .WithColumn(nameof(WithdrawalRequest.Amount)).AsDecimal(18, 4)
                .WithColumn(nameof(WithdrawalRequest.StatusId)).AsInt32().Indexed()
                .WithColumn(nameof(WithdrawalRequest.AdminNotes)).AsString(1000).Nullable()
                .WithColumn(nameof(WithdrawalRequest.CreatedOnUtc)).AsDateTime2()
                .WithColumn(nameof(WithdrawalRequest.UpdatedOnUtc)).AsDateTime2();
        }
    }

    
}