using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Images;

public class UploadLotImageEndpoint : EndpointWithoutRequest
{
    private readonly IImageStorageService _storageService;
    private readonly ILotImageRepository _imageRepository;
    private readonly ILotRepository _lotRepository;

    public UploadLotImageEndpoint(
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
        Post("/api/lots/{id}/images");
        Roles("User");
        AllowFileUploads();
        Summary(s =>
        {
            s.Summary = "Upload image for a lot";
            s.Description = "Upload one or more images for an owned Draft or Rejected lot.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

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
            ThrowError("Only the lot owner can upload images", 403);
            return;
        }

        if (!lot.IsEditable)
        {
            ThrowError("Images can only be changed for draft or rejected lots", 409);
            return;
        }

        var files = HttpContext.Request.Form.Files;
        if (files.Count == 0)
        {
            ThrowError("No files uploaded", 400);
            return;
        }

        var uploadedImages = new List<object>();

        foreach (var file in files)
        {
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType))
            {
                ThrowError($"File type '{file.ContentType}' is not allowed. Allowed: jpeg, png, webp, gif", 400);
                return;
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                ThrowError($"File '{file.FileName}' exceeds 5MB limit", 400);
                return;
            }

            var extension = Path.GetExtension(file.FileName);
            var objectName = $"lots/{lotId}/{Guid.NewGuid()}{extension}";

            using var stream = file.OpenReadStream();
            await _storageService.UploadAsync(stream, objectName, file.ContentType, ct);

            var image = LotImage.Create(lotId, file.FileName, objectName, file.ContentType, file.Length);
            lot.AddImage(image);
            await _imageRepository.AddAsync(image, ct);
            await _imageRepository.SaveChangesAsync(ct);

            var presignedUrl = await _storageService.GetPresignedUrlAsync(objectName, 60, ct);

            uploadedImages.Add(new
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
        await HttpContext.Response.WriteAsJsonAsync(uploadedImages, ct);
    }
}
