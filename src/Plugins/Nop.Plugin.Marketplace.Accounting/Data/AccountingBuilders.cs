using System.Collections.Generic;
using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Accounting.Domains;

namespace Nop.Plugin.Marketplace.Accounting.Data
{
    public class GlAccountBuilder : NopEntityBuilder<GlAccount>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(GlAccount.AccountCode)).AsString(50).Indexed().Unique()
                .WithColumn(nameof(GlAccount.Name)).AsString(255).NotNullable()
                .WithColumn(nameof(GlAccount.AccountTypeId)).AsInt32().Indexed()
                .WithColumn(nameof(GlAccount.IsActive)).AsBoolean().WithDefaultValue(true);
        }
    }

    public class JournalEntryBuilder : NopEntityBuilder<JournalEntry>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(JournalEntry.TransactionDateUtc)).AsDateTime2()
                .WithColumn(nameof(JournalEntry.ReferenceId)).AsString(255).Indexed()
                .WithColumn(nameof(JournalEntry.Memo)).AsString(1000).Nullable()
                .WithColumn(nameof(JournalEntry.IdempotencyKey)).AsString(255).Unique()
                .WithColumn(nameof(JournalEntry.CreatedOnUtc)).AsDateTime2();
        }
    }

    public class JournalEntryLineBuilder : NopEntityBuilder<JournalEntryLine>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(JournalEntryLine.JournalEntryId)).AsInt32().Indexed()
                .WithColumn(nameof(JournalEntryLine.GlAccountId)).AsInt32().Indexed()
                .WithColumn(nameof(JournalEntryLine.DebitAmount)).AsDecimal(18, 4)
                .WithColumn(nameof(JournalEntryLine.CreditAmount)).AsDecimal(18, 4)
                .WithColumn(nameof(JournalEntryLine.VendorId)).AsInt32().Nullable().Indexed()
                .WithColumn(nameof(JournalEntryLine.OrderId)).AsInt32().Nullable().Indexed();
        }
    }

    [NopMigration("2026/06/13 14:00:00:0000000", "Marketplace.Accounting base schema", MigrationProcessType.Installation)]
    public class AccountingSchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<GlAccount>();
            Create.TableFor<JournalEntry>();
            Create.TableFor<JournalEntryLine>();

            // ALIBABA-GRADE: Comprehensive Default Chart of Accounts (CoA)
            Insert.IntoTable(nameof(GlAccount)).Row(new { AccountCode = "1000", Name = "Gateway Clearing Account", AccountTypeId = 10, IsActive = true });
            Insert.IntoTable(nameof(GlAccount)).Row(new { AccountCode = "1010", Name = "Corporate Bank Account", AccountTypeId = 10, IsActive = true });
            Insert.IntoTable(nameof(GlAccount)).Row(new { AccountCode = "2000", Name = "Escrow Liability (Unsettled)", AccountTypeId = 20, IsActive = true });
            Insert.IntoTable(nameof(GlAccount)).Row(new { AccountCode = "2010", Name = "Vendor Payables (Wallet Available)", AccountTypeId = 20, IsActive = true });
            Insert.IntoTable(nameof(GlAccount)).Row(new { AccountCode = "2020", Name = "Vendor Rolling Reserve", AccountTypeId = 20, IsActive = true });
            Insert.IntoTable(nameof(GlAccount)).Row(new { AccountCode = "4000", Name = "Marketplace Commission Revenue", AccountTypeId = 40, IsActive = true });

            // NEW: Platform Loss / Refund Expense Account
            Insert.IntoTable(nameof(GlAccount)).Row(new { AccountCode = "5000", Name = "Refund & Loss Expense", AccountTypeId = 50, IsActive = true });
        }
    }
}