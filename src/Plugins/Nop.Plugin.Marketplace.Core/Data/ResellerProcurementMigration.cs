using FluentMigrator;
using Nop.Data.Migrations;

namespace Nop.Plugin.Marketplace.Core.Data
{
    [NopMigration("2026/06/13 10:05:00", "Add Policy to ResellerMapping", MigrationProcessType.Update)]
    public class ResellerProcurementMigration : ForwardOnlyMigration
    {
        public override void Up()
        {
            Alter.Table("ResellerProductMapping")
                .AddColumn("SelectedProcurementPolicyId").AsInt32().WithDefaultValue(1);
        }
    }
}