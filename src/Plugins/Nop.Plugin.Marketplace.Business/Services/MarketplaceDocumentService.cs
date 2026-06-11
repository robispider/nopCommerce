using Minio;
using Minio.DataModel.Args;
using Nop.Plugin.Marketplace.Business.Settings;

namespace Nop.Plugin.Marketplace.Business.Services
{
    public class MarketplaceDocumentService : IMarketplaceDocumentService
    {
        private readonly MarketplaceBusinessSettings _settings;

        public MarketplaceDocumentService(MarketplaceBusinessSettings settings)
        {
            _settings = settings;
        }

        public async Task<string> UploadKycDocumentAsync(Stream fileStream, string fileName, string contentType)
        {
            var minio = new MinioClient()
                .WithEndpoint(_settings.MinioEndpoint)
                .WithCredentials(_settings.MinioAccessKey, _settings.MinioSecretKey)
                .WithSSL(_settings.UseSSL)
                .Build();

            // Ensure bucket exists
            var bktArgs = new BucketExistsArgs().WithBucket(_settings.KycBucketName);
            bool found = await minio.BucketExistsAsync(bktArgs);
            if (!found)
            {
                await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_settings.KycBucketName));
            }

            // Generate unique object name
            var objectName = $"{Guid.NewGuid()}-{fileName}";

            var putArgs = new PutObjectArgs()
                .WithBucket(_settings.KycBucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await minio.PutObjectAsync(putArgs);

            // Return internal URI for database tracking
            return $"minio://{_settings.KycBucketName}/{objectName}";
        }
    }
}