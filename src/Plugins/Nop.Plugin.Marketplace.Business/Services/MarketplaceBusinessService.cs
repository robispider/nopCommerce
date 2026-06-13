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

            var customer = (await _customerService.GetAllCustomersAsync(vendorId: business.VendorId)).FirstOrDefault();
            if (customer != null)
            {
                // 1. Resolve Native Roles
                var supplierRole = await _customerService.GetCustomerRoleBySystemNameAsync("MarketplaceSupplier");
                var resellerRole = await _customerService.GetCustomerRoleBySystemNameAsync("MarketplaceReseller");

                // 2. Map dynamically based on KYC selection
                if (business.RoleTypeId == (int)MarketplaceRoleType.Supplier || business.RoleTypeId == (int)MarketplaceRoleType.Both)
                {
                    await _customerService.AddCustomerRoleMappingAsync(new CustomerCustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = supplierRole.Id });
                }

                if (business.RoleTypeId == (int)MarketplaceRoleType.Reseller || business.RoleTypeId == (int)MarketplaceRoleType.Both)
                {
                    await _customerService.AddCustomerRoleMappingAsync(new CustomerCustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = resellerRole.Id });
                }
            }
        }
    }
}