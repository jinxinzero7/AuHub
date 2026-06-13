using Identity.Application.Services;
using Minio;
using Minio.DataModel.Args;

namespace Identity.Infrastructure.Services;

public class MinioDocumentStorageService : IDocumentStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public MinioDocumentStorageService(IMinioClient minioClient, string bucketName)
    {
        _minioClient = minioClient;
        _bucketName = bucketName;
    }

    public async Task InitializeBucketAsync(CancellationToken ct = default)
    {
        var bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName), ct);

        if (!bucketExists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucketName), ct);
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string objectName, string contentType, CancellationToken ct = default)
    {
        await _minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType), ct);

        return objectName;
    }

    public async Task<(Stream Stream, string ContentType, long Size)> GetStreamAsync(string objectName, CancellationToken ct = default)
    {
        var stat = await _minioClient.StatObjectAsync(
            new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName), ct);

        var memoryStream = new MemoryStream();
        await _minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream)), ct);

        memoryStream.Position = 0;
        return (memoryStream, stat.ContentType, stat.Size);
    }

    public async Task DeleteAsync(string objectName, CancellationToken ct = default)
    {
        await _minioClient.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName), ct);
    }
}
