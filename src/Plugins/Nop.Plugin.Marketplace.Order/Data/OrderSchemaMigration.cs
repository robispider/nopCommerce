using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Order.Domains;

namespace Nop.Plugin.Marketplace.Order.Data
{
    [NopMigration("2026/01/01 01:00:00:0000000", "Marketplace.Order base schema", MigrationProcessType.Installation)]
    public class OrderSchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<MarketplaceOrderGroup>();
            Create.TableFor<MarketplaceOrderAllocation>();
        }
    }
}