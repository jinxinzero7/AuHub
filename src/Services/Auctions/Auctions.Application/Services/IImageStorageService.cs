namespace Auctions.Application.Services;

public interface IImageStorageService
{
    Task<string> UploadAsync(Stream fileStream, string objectName, string contentType, CancellationToken ct = default);
    Task<string> GetPresignedUrlAsync(string objectName, int expiresMinutes = 60, CancellationToken ct = default);
    Task DeleteAsync(string objectName, CancellationToken ct = default);
    Task InitializeBucketAsync(CancellationToken ct = default);
}
