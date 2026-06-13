using FluentMigrator;
using Nop.Data.Migrations;

namespace Nop.Plugin.Marketplace.Dropship.Data
{
    [NopMigration("2026/06/13 10:10:00", "Add Policy to DropshipTicket", MigrationProcessType.Update)]
    public class TicketProcurementMigration : ForwardOnlyMigration
    {
        public override void Up()
        {
            Alter.Table("DropshipFulfillment")
                .AddColumn("ProcurementPolicyId").AsInt32().WithDefaultValue(1);
        }
    }
}