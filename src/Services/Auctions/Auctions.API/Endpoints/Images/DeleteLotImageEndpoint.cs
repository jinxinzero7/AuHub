using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Images;

public class DeleteLotImageEndpoint : EndpointWithoutRequest
{
    private readonly IImageStorageService _storageService;
    private readonly ILotImageRepository _imageRepository;
    private readonly ILotRepository _lotRepository;

    public DeleteLotImageEndpoint(
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
        Delete("/api/lots/{lotId}/images/{imageId}");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Delete image from a lot";
            s.Description = "Delete an image from an auction lot. Owner only.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var imageId = Route<Guid>("imageId");
        var lotId = Route<Guid>("lotId");

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID in token", 401);
            return;
        }

        var lot = await _lotRepository.GetByIdAsync(lotId, ct);
        if (lot == null)
        {
            ThrowError("Lot not found", 404);
            return;
        }

        if (lot.SellerId != userId)
        {
            ThrowError("Only the lot owner can delete images", 403);
            return;
        }

        var image = await _imageRepository.GetByIdAsync(imageId, ct);
        if (image == null)
        {
            ThrowError("Image not found", 404);
            return;
        }

        if (image.LotId != lotId)
        {
            ThrowError("Image not found", 404);
            return;
        }

        await _storageService.DeleteAsync(image.ObjectName, ct);

        _imageRepository.Remove(image);
        await _imageRepository.SaveChangesAsync(ct);

        HttpContext.Response.StatusCode = 200;
        await HttpContext.Response.WriteAsJsonAsync(new { success = true }, ct);
    }
}
