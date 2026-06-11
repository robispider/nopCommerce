using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Marketplace.Storefront.Domains;

namespace Nop.Plugin.Marketplace.Storefront.Data
{
    // The timestamp ensures this runs exactly when installed.
    [NopMigration("2026/06/10 16:45:00:0000000", "Marketplace.Storefront base schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            // This safely generates the table using the rules defined in StorefrontBuilder
            Create.TableFor<ResellerStorefront>();
        }
    }
}