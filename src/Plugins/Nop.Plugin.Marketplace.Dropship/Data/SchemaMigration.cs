using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Dropship.Domains;

namespace Nop.Plugin.Marketplace.Dropship.Data
{
    [NopMigration("2026-06-12 18:00:00", "Marketplace.Dropship base schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<DropshipFulfillment>();
        }
    }
}