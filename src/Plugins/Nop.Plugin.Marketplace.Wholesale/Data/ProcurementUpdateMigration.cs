using FluentMigrator;
using Nop.Data.Migrations;

namespace Nop.Plugin.Marketplace.Wholesale.Data
{
    [NopMigration("2026/06/13 10:00:00", "Add Procurement Policies to SupplierProduct", MigrationProcessType.Update)]
    public class ProcurementUpdateMigration : ForwardOnlyMigration
    {
        public override void Up()
        {
            Alter.Table("SupplierProduct")
                .AddColumn("AllowedProcurementPolicies").AsInt32().WithDefaultValue(1); // Defaults to FullEscrow
        }
    }
}