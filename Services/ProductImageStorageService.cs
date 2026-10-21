using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzureOmsDemo.Services
{
    public sealed class ProductImageStorageService
    {
        private readonly BlobContainerClient _container;

        public ProductImageStorageService(
            BlobServiceClient blobServiceClient,
            IConfiguration configuration)
        {
            var containerName = configuration[
                "AzureBlob:ProductImagesContainer"]
                ?? throw new InvalidOperationException(
                    "Missing ProductImagesContainer.");

            _container = blobServiceClient
                .GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            var blobName = $"{Guid.NewGuid():N}{extension}";
            var blobClient = _container.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();

            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = extension == ".png"
                        ? "image/png"
                        : "image/jpeg"
                    }
                },
                cancellationToken);
            return blobName;
        }

        public async Task<BlobDownloadStreamingResult?> DownloadAsync(
            string blobName,
            CancellationToken cancellationToken = default)
        {
            var blobClient = _container.GetBlobClient(blobName);

            try
            {
                return await blobClient.DownloadStreamingAsync(
                    cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return null;
            }
        }

    }
}
