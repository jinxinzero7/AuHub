using Minio;
using Minio.DataModel.Args;
using Auctions.Application.Services;

namespace Auctions.Infrastructure.Storage;

public class MinioImageStorageService : IImageStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public MinioImageStorageService(IMinioClient minioClient, string bucketName)
    {
        _minioClient = minioClient;
        _bucketName = bucketName;
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

        return await _minioClient.PresignedGetObjectAsync(args);
    }

    public async Task DeleteAsync(string objectName, CancellationToken ct = default)
    {
        await _minioClient.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName));
    }
}
