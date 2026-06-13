namespace Identity.Application.Services;

public interface IDocumentStorageService
{
    Task InitializeBucketAsync(CancellationToken ct = default);
    Task<string> UploadAsync(Stream fileStream, string objectName, string contentType, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType, long Size)> GetStreamAsync(string objectName, CancellationToken ct = default);
    Task DeleteAsync(string objectName, CancellationToken ct = default);
}
