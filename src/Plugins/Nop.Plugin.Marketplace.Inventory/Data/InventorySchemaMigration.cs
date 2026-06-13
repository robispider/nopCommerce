using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Inventory.Domains;

namespace Nop.Plugin.Marketplace.Inventory.Data
{
    [NopMigration("2026/01/01 00:00:00:0000000", "Marketplace.Inventory base schema", MigrationProcessType.Installation)]
    public class InventorySchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<InventoryBucket>();
            Create.TableFor<StockReservation>();
        }
    }
}