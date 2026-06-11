using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Core.Domains;

namespace Nop.Plugin.Marketplace.Core.Data
{
    [NopMigration("2026/06/09 12:00:00:0000000", "Marketplace.Core base schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            // Creates the tables using the configurations defined in our Builders
            Create.TableFor<MarketplaceBusiness>();
            Create.TableFor<ResellerProductMapping>();
        }
    }
}