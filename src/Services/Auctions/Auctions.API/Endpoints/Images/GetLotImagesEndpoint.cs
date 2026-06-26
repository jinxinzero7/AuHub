using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using Auctions.Application.Options;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Images;

public class GetLotImagesEndpoint : EndpointWithoutRequest
{
    private readonly ILotImageRepository _imageRepository;
    private readonly ILotRepository _lotRepository;
    private readonly ExternalUrlOptions _externalUrl;

    public GetLotImagesEndpoint(
        ILotImageRepository imageRepository,
        ILotRepository lotRepository,
        IOptions<ExternalUrlOptions> externalUrl)
    {
        _imageRepository = imageRepository;
        _lotRepository = lotRepository;
        _externalUrl = externalUrl.Value;
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

        var result = images.Select(image => new
        {
            image.Id,
            image.FileName,
            Url = $"{_externalUrl.StorageUrl.TrimEnd('/')}/{image.ObjectName}",
            image.ContentType,
            image.Size,
            image.UploadedAt
        }).ToList<object>();

        HttpContext.Response.StatusCode = 200;
        await HttpContext.Response.WriteAsJsonAsync(result, ct);
    }

    private Guid? GetRequesterUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
