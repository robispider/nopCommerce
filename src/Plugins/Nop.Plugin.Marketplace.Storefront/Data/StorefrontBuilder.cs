using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Storefront.Domains;

namespace Nop.Plugin.Marketplace.Storefront.Data
{
    /// <summary>
    /// Configures the database schema and indexes for the ResellerStorefront table.
    /// </summary>
    public class StorefrontBuilder : NopEntityBuilder<ResellerStorefront>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(ResellerStorefront.StoreName)).AsString(400).NotNullable()

                // UrlSlug is critical for routing. Must be indexed. Length 200 is plenty for slugs.
                .WithColumn(nameof(ResellerStorefront.UrlSlug)).AsString(200).Nullable().Indexed()

                // CustomDomain is also critical for routing lookups.
                .WithColumn(nameof(ResellerStorefront.CustomDomain)).AsString(400).Nullable().Indexed()

                .WithColumn(nameof(ResellerStorefront.PrimaryColorHex)).AsString(50).Nullable()

                // Index VendorId because we will frequently query "Get Storefront By VendorId" in the admin panel
                .WithColumn(nameof(ResellerStorefront.VendorId)).AsInt32().Indexed();
        }
    }
}