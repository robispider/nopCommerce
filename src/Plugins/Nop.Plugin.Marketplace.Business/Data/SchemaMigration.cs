using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Business.Domains;

namespace Nop.Plugin.Marketplace.Business.Data
{
    [NopMigration("2026/06/10 12:00:00:0000000", "Marketplace.Business base schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<BusinessDocument>();
        }
    }
}