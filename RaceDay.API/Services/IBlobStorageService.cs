namespace RaceDay.API.Services
{
    public interface IBlobStorageService
    {
        Task<string> UploadImageAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);

        Task<(Stream Stream, string ContentType)> DownloadImageAsync(
            string blobName,
            CancellationToken cancellationToken = default);
    }
}