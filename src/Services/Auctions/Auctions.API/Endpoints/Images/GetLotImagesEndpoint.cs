using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;

namespace Auctions.API.Endpoints.Images;

public class GetLotImagesEndpoint : EndpointWithoutRequest
{
    private readonly IImageStorageService _storageService;
    private readonly ILotImageRepository _imageRepository;

    public GetLotImagesEndpoint(
        IImageStorageService storageService,
        ILotImageRepository imageRepository)
    {
        _storageService = storageService;
        _imageRepository = imageRepository;
    }

    public override void Configure()
    {
        Get("/api/lots/{id}/images");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get images for a lot";
            s.Description = "Retrieve all images for an auction lot with presigned URLs.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        var images = await _imageRepository.GetByLotIdAsync(lotId, ct);

        var result = new List<object>();
        foreach (var image in images)
        {
            var presignedUrl = await _storageService.GetPresignedUrlAsync(image.ObjectName, 60, ct);
            result.Add(new
            {
                image.Id,
                image.FileName,
                Url = presignedUrl,
                image.ContentType,
                image.Size,
                image.UploadedAt
            });
        }

        HttpContext.Response.StatusCode = 200;
        await HttpContext.Response.WriteAsJsonAsync(result, ct);
    }
}
