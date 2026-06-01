using FluentMigrator;
using Nop.Data.Migrations;
using Nop.Web.Framework.Extensions;

namespace Nop.Plugin.Payments.Manual.Migrations;

[NopMigration("2026/06/01 13:30:00", "Nop.Plugin.Payments.Manual 5.0.4 localization migration", MigrationProcessType.Update)]
public class GoogleAuthenticatorSchemaMigration : MigrationBase
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        this.AddOrUpdateLocaleResource(new Dictionary<string, string>
        {
            ["Plugins.Payments.Manual.SaveCCError"] = "An error occurred while saving credit card info. Please try again or check the logs for more details about a problem."
        });
    }

    /// <summary>Collects the DOWN migration expressions</summary>
    public override void Down()
    {
        //add the downgrade logic if necessary 
    }
}