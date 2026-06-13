using System;
using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Commission.Domains;
using Nop.Plugin.Marketplace.Commission.Domains.Enums;

namespace Nop.Plugin.Marketplace.Commission.Data
{
    [NopMigration("2026/01/01 02:00:00:0000000", "Marketplace.Commission base schema", MigrationProcessType.Installation)]
    public class CommissionSchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<CommissionRule>();
            Create.TableFor<CommissionSplit>();

            // Idempotency: Ensure no duplicate splits for the same item
            Create.UniqueConstraint("UQ_CommissionSplit_NativeOrder_OrderItem")
                .OnTable(nameof(CommissionSplit))
                .Columns(nameof(CommissionSplit.NativeOrderId), nameof(CommissionSplit.OrderItemId));

            // Seed the Global Default Rule immediately!
            Insert.IntoTable(nameof(CommissionRule)).Row(new
            {
                Name = "Global Platform Default (5%)",
                PriorityId = (int)CommissionPriority.GlobalDefault,
                Percentage = 5.00m,
                FixedAmount = 0.00m,
                IsActive = true,
                CreatedOnUtc = DateTime.UtcNow
            });
        }
    }
}