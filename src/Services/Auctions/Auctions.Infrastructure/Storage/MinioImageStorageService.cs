using Minio;
using Minio.DataModel.Args;
using Auctions.Application.Services;

namespace Auctions.Infrastructure.Storage;

public class MinioImageStorageService : IImageStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly IMinioClient _presignClient;
    private readonly string _bucketName;

    public MinioImageStorageService(
        IMinioClient minioClient,
        string bucketName,
        string externalEndpoint,
        string accessKey,
        string secretKey)
    {
        _minioClient = minioClient;
        _bucketName = bucketName;

        if (!string.IsNullOrEmpty(externalEndpoint))
        {
            var uri = new Uri(externalEndpoint);
            _presignClient = new MinioClient()
                .WithEndpoint(uri.Host, uri.Port)
                .WithCredentials(accessKey, secretKey)
                .Build();
        }
        else
        {
            _presignClient = minioClient;
        }
    }

    public async Task InitializeBucketAsync(CancellationToken ct = default)
    {
        var bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName));

        if (!bucketExists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucketName));
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
                .WithContentType(contentType));

        return objectName;
    }

    public async Task<string> GetPresignedUrlAsync(string objectName, int expiresMinutes = 60, CancellationToken ct = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithExpiry(expiresMinutes * 60);

        return await _presignClient.PresignedGetObjectAsync(args);
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
                .WithObject(objectName));
    }
}
