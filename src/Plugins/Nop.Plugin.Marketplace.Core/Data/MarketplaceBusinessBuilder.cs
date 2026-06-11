using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Core.Domains;

namespace Nop.Plugin.Marketplace.Core.Data
{
    public class MarketplaceBusinessBuilder : NopEntityBuilder<MarketplaceBusiness>
    {
        // Changed from Map() to MapEntity() for nopCommerce 4.60+ compatibility
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(MarketplaceBusiness.LegalName)).AsString(400).NotNullable()
                .WithColumn(nameof(MarketplaceBusiness.TaxId)).AsString(100).Nullable()
                // Indexed for fast lookups when resolving current vendor requests
                .WithColumn(nameof(MarketplaceBusiness.VendorId)).AsInt32().Indexed();
        }
    }
}