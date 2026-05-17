using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;

namespace Auctions.API.Endpoints.Images;

public class DeleteLotImageEndpoint : EndpointWithoutRequest
{
    private readonly IImageStorageService _storageService;
    private readonly ILotImageRepository _imageRepository;

    public DeleteLotImageEndpoint(
        IImageStorageService storageService,
        ILotImageRepository imageRepository)
    {
        _storageService = storageService;
        _imageRepository = imageRepository;
    }

    public override void Configure()
    {
        Delete("/api/lots/{lotId}/images/{imageId}");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Delete image from a lot";
            s.Description = "Delete an image from an auction lot. Admin only.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var imageId = Route<Guid>("imageId");

        var image = await _imageRepository.GetByIdAsync(imageId, ct);
        if (image == null)
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
