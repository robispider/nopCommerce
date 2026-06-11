using Nop.Plugin.Marketplace.Core.Domains;

namespace Nop.Plugin.Marketplace.Business.Services
{
    public interface IMarketplaceBusinessService
    {
        Task SubmitKycAsync(int vendorId, string legalName, string taxId, Stream docStream, string docName, string mimeType);
        Task ApproveBusinessAsync(int marketplaceBusinessId);
    }
}