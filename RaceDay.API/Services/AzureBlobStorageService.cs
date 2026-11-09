using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services
{
    public class AzureBlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageService(IConfiguration configuration)
        {
            var connectionString =
                configuration["AzureBlobStorage:ConnectionString"];

            var containerName =
                configuration["AzureBlobStorage:ContainerName"];

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Azure Blob Storage connection string is missing.");

            if (string.IsNullOrWhiteSpace(containerName))
                throw new InvalidOperationException(
                    "Azure Blob Storage container name is missing.");

            var serviceClient =
                new BlobServiceClient(connectionString);

            _containerClient =
                serviceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadImageAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            await _containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None,
                cancellationToken: cancellationToken);

            var safeFileName = Path.GetFileName(fileName);
            var blobName = $"{Guid.NewGuid():N}_{safeFileName}";

            var blobClient = _containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                },
                cancellationToken);

            return blobClient.Uri.ToString();
        }

        public async Task<(Stream Stream, string ContentType)> DownloadImageAsync(
            string blobName,
            CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            var download = await blobClient.DownloadStreamingAsync(
                cancellationToken: cancellationToken);

            var contentType =
                download.Value.Details.ContentType
                ?? "application/octet-stream";

            return (download.Value.Content, contentType);
        }
    }
}