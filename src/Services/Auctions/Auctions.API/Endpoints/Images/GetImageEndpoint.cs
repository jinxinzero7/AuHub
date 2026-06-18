using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Images;

public class GetImageEndpoint : EndpointWithoutRequest
{
    private readonly IImageStorageService _storageService;
    private readonly ILotImageRepository _imageRepository;
    private readonly ILotRepository _lotRepository;

    public GetImageEndpoint(
        IImageStorageService storageService,
        ILotImageRepository imageRepository,
        ILotRepository lotRepository)
    {
        _storageService = storageService;
        _imageRepository = imageRepository;
        _lotRepository = lotRepository;
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

        var lot = await _lotRepository.GetByIdAsync(lotId, ct);
        if (lot == null || !LotVisibilityPolicy.CanViewDetails(lot, GetRequesterUserId(), User.IsInRole("Admin")))
        {
            HttpContext.Response.StatusCode = 404;
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

    private Guid? GetRequesterUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
