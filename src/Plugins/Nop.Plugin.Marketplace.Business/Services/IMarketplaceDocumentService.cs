namespace Nop.Plugin.Marketplace.Business.Services
{
    public interface IMarketplaceDocumentService
    {
        Task<string> UploadKycDocumentAsync(Stream fileStream, string fileName, string contentType);
    }
}