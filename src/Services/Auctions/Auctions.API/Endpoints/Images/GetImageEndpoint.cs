using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;

namespace Auctions.API.Endpoints.Images;

public class GetImageEndpoint : EndpointWithoutRequest
{
    private readonly IImageStorageService _storageService;
    private readonly ILotImageRepository _imageRepository;

    public GetImageEndpoint(
        IImageStorageService storageService,
        ILotImageRepository imageRepository)
    {
        _storageService = storageService;
        _imageRepository = imageRepository;
    }

    public override void Configure()
    {
        Get("/api/lots/{id}/images/{fileName}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get lot image";
            s.Description = "Redirect to a pre-signed MinIO URL.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");
        var fileName = Route<string?>("fileName");

        if (string.IsNullOrEmpty(fileName))
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        var image = await _imageRepository.GetByFileNameAsync(lotId, fileName, ct);
        if (image == null)
        {
            HttpContext.Response.StatusCode = 404;
            return;
        }

        var presignedUrl = await _storageService.GetPresignedUrlAsync(image.ObjectName, 1440, ct);
        HttpContext.Response.Redirect(presignedUrl, permanent: false);
    }
}
