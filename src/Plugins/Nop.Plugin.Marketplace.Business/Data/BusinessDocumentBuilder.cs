using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Marketplace.Business.Domains;

namespace Nop.Plugin.Marketplace.Business.Data
{
    public class BusinessDocumentBuilder : NopEntityBuilder<BusinessDocument>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(BusinessDocument.DocumentType)).AsString(100).NotNullable()
                .WithColumn(nameof(BusinessDocument.FileUri)).AsString(1000).NotNullable()
                .WithColumn(nameof(BusinessDocument.MimeType)).AsString(100).NotNullable()
                .WithColumn(nameof(BusinessDocument.MarketplaceBusinessId)).AsInt32().Indexed();
        }
    }
}