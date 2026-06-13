using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Wholesale.Domains;

namespace Nop.Plugin.Marketplace.Wholesale.Data
{
    public class SupplierProductBuilder : NopEntityBuilder<SupplierProduct>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                // Indexes are crucial here for fast catalog rendering
                .WithColumn(nameof(SupplierProduct.ProductId)).AsInt32().Indexed()
                .WithColumn(nameof(SupplierProduct.VendorId)).AsInt32().Indexed()

                // Money columns should use decimal(18, 4) in nopCommerce
                .WithColumn(nameof(SupplierProduct.WholesalePrice)).AsDecimal(18, 4)

                .WithColumn(nameof(SupplierProduct.MinimumOrderQuantity)).AsInt32()
                .WithColumn(nameof(SupplierProduct.IsDropshipEnabled)).AsBoolean()
                .WithColumn(nameof(SupplierProduct.IsPreorderEnabled)).AsBoolean()
                .WithColumn(nameof(SupplierProduct.LeadTimeDays)).AsInt32();
        }
    }
}