using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Core.Domains;

namespace Nop.Plugin.Marketplace.Core.Data
{
    public class ResellerProductMappingBuilder : NopEntityBuilder<ResellerProductMapping>
    {
        // Changed from Map() to MapEntity() for nopCommerce 4.60+ compatibility
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(ResellerProductMapping.MarginPercentage)).AsDecimal(18, 4).NotNullable()
                // Indexes are crucial here to ensure lightning-fast Cart Validation and Price Syncing
                .WithColumn(nameof(ResellerProductMapping.ResellerCoreProductId)).AsInt32().Indexed()
                .WithColumn(nameof(ResellerProductMapping.SupplierCoreProductId)).AsInt32().Indexed();
        }
    }
}