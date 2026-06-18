using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Images;

public class GetLotImagesEndpoint : EndpointWithoutRequest
{
    private readonly IImageStorageService _storageService;
    private readonly ILotImageRepository _imageRepository;
    private readonly ILotRepository _lotRepository;

    public GetLotImagesEndpoint(
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
        Get("/api/lots/{id}/images");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get images for a lot";
            s.Description = "Retrieve all images for an auction lot with pre-signed URLs.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");
        var lot = await _lotRepository.GetByIdAsync(lotId, ct);
        if (lot == null || !LotVisibilityPolicy.CanViewDetails(lot, GetRequesterUserId(), User.IsInRole("Admin")))
        {
            HttpContext.Response.StatusCode = 404;
            return;
        }

        var images = await _imageRepository.GetByLotIdAsync(lotId, ct);

        var result = new List<object>();
        foreach (var image in images)
        {
            var url = await _storageService.GetPresignedUrlAsync(image.ObjectName, 1440, ct);
            result.Add(new
            {
                image.Id,
                image.FileName,
                Url = url,
                image.ContentType,
                image.Size,
                image.UploadedAt
            });
        }

        HttpContext.Response.StatusCode = 200;
        await HttpContext.Response.WriteAsJsonAsync(result, ct);
    }

    private Guid? GetRequesterUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
