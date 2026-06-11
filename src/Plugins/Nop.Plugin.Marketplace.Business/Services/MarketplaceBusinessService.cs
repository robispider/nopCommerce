using Nop.Core.Domain.Customers;
using Nop.Data;
using Nop.Plugin.Marketplace.Business.Domains;
using Nop.Plugin.Marketplace.Core.Domains;
using Nop.Services.Customers;

namespace Nop.Plugin.Marketplace.Business.Services
{
    public class MarketplaceBusinessService : IMarketplaceBusinessService
    {
        private readonly IRepository<MarketplaceBusiness> _businessRepository;
        private readonly IRepository<BusinessDocument> _documentRepository;
        private readonly IMarketplaceDocumentService _documentService;
        private readonly ICustomerService _customerService;

        public MarketplaceBusinessService(
            IRepository<MarketplaceBusiness> businessRepository,
            IRepository<BusinessDocument> documentRepository,
            IMarketplaceDocumentService documentService,
            ICustomerService customerService)
        {
            _businessRepository = businessRepository;
            _documentRepository = documentRepository;
            _documentService = documentService;
            _customerService = customerService;
        }

        public async Task SubmitKycAsync(int vendorId, string legalName, string taxId, Stream docStream, string docName, string mimeType)
        {
            // 1. Upload to MinIO
            var fileUri = await _documentService.UploadKycDocumentAsync(docStream, docName, mimeType);

            // 2. Create Business Record
            var business = new MarketplaceBusiness
            {
                VendorId = vendorId,
                LegalName = legalName,
                TaxId = taxId,
                VerificationStatusId = (int)BusinessVerificationStatus.Pending,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await _businessRepository.InsertAsync(business);

            // 3. Save Document DB Entry
            var doc = new BusinessDocument
            {
                MarketplaceBusinessId = business.Id,
                DocumentType = "Registration",
                FileUri = fileUri,
                MimeType = mimeType,
                UploadedOnUtc = DateTime.UtcNow
            };
            await _documentRepository.InsertAsync(doc);
        }

        public async Task ApproveBusinessAsync(int marketplaceBusinessId)
        {
            var business = await _businessRepository.GetByIdAsync(marketplaceBusinessId);
            if (business == null)
                throw new Exception("Business not found");

            business.VerificationStatusId = (int)BusinessVerificationStatus.Approved;
            business.UpdatedOnUtc = DateTime.UtcNow;
            await _businessRepository.UpdateAsync(business);

            // Fetch the nopCommerce customer mapped to this vendor
            var customer = (await _customerService.GetAllCustomersAsync(vendorId: business.VendorId)).FirstOrDefault();
            if (customer != null)
            {
                // Assign Supplier Role (Assuming they applied for Supplier, can be dynamic based on Business.RoleTypeId)
                var supplierRole = await _customerService.GetCustomerRoleBySystemNameAsync("MarketplaceSupplier");
                if (supplierRole != null)
                {
                    await _customerService.AddCustomerRoleMappingAsync(new CustomerCustomerRoleMapping
                    {
                        CustomerId = customer.Id,
                        CustomerRoleId = supplierRole.Id
                    });
                }
            }
        }
    }
}