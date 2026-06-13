using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Wholesale.Domains;

namespace Nop.Plugin.Marketplace.Wholesale.Data
{
    [NopMigration("2026-06-12 17:00:00", "Marketplace.Wholesale base schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<SupplierProduct>();
        }
    }
}