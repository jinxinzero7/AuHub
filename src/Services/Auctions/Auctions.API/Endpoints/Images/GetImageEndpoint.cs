using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using System.Security.Claims;
using Auctions.Domain.Entities;

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
        Get("/api/lots/{id}/images/{imageId}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get lot image";
            s.Description = "Stream image from storage via backend proxy.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");
        var imageId = Route<Guid>("imageId");

        var lot = await _lotRepository.GetByIdAsync(lotId, ct);
        if (lot == null || !LotVisibilityPolicy.CanViewDetails(lot, GetRequesterUserId(), User.IsInRole("Admin")))
        {
            HttpContext.Response.StatusCode = 404;
            await HttpContext.Response.Body.FlushAsync(ct);
            return;
        }

        var image = await _imageRepository.GetByIdAsync(imageId, ct);
        if (image == null || image.LotId != lotId)
        {
            HttpContext.Response.StatusCode = 404;
            await HttpContext.Response.Body.FlushAsync(ct);
            return;
        }

        try
        {
            var (stream, contentType, size) = await _storageService.GetStreamAsync(image.ObjectName, ct);
            HttpContext.Response.StatusCode = 200;
            HttpContext.Response.ContentType = contentType;
            HttpContext.Response.ContentLength = size;
            await stream.CopyToAsync(HttpContext.Response.Body, ct);
            await HttpContext.Response.Body.FlushAsync(ct);
        }
        catch
        {
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.StatusCode = 500;
                await HttpContext.Response.Body.FlushAsync(ct);
            }
        }
    }

    private Guid? GetRequesterUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
