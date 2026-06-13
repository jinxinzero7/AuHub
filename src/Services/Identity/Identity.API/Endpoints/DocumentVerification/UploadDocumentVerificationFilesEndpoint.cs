using System.Security.Claims;
using FastEndpoints;
using Identity.Application.Services;

namespace Identity.API.Endpoints.DocumentVerification;

public class UploadDocumentVerificationFilesEndpoint : EndpointWithoutRequest<DocumentVerificationUploadResponse>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly IDocumentStorageService _documentStorageService;

    public UploadDocumentVerificationFilesEndpoint(IDocumentStorageService documentStorageService)
    {
        _documentStorageService = documentStorageService;
    }

    public override void Configure()
    {
        Post("/api/auth/document-verification/upload");
        Roles("User");
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user token", 401);
            return;
        }

        var files = HttpContext.Request.Form.Files;
        var passportImage = files.GetFile("passportImage");
        var selfieImage = files.GetFile("selfieImage");

        if (passportImage == null || selfieImage == null)
        {
            ThrowError("Passport image and selfie image are required", 400);
            return;
        }

        ValidateFile(passportImage, "passportImage");
        ValidateFile(selfieImage, "selfieImage");
        ThrowIfAnyErrors();

        var passportImagePath = await UploadFileAsync(userId, "passport", passportImage, ct);
        var selfieImagePath = await UploadFileAsync(userId, "selfie", selfieImage, ct);

        Response = new DocumentVerificationUploadResponse
        {
            PassportImagePath = passportImagePath,
            SelfieImagePath = selfieImagePath
        };
    }

    private void ValidateFile(IFormFile file, string fieldName)
    {
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            AddError(fieldName, "Only jpeg, png and webp images are allowed");
        }

        if (file.Length <= 0)
        {
            AddError(fieldName, "File is empty");
        }

        if (file.Length > 8 * 1024 * 1024)
        {
            AddError(fieldName, "File exceeds 8MB limit");
        }
    }

    private async Task<string> UploadFileAsync(Guid userId, string kind, IFormFile file, CancellationToken ct)
    {
        var extension = GetExtension(file.ContentType);
        var objectName = $"document-verifications/{userId}/{kind}-{Guid.NewGuid()}{extension}";

        using var stream = file.OpenReadStream();
        return await _documentStorageService.UploadAsync(stream, objectName, file.ContentType, ct);
    }

    private static string GetExtension(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
    }
}

public record DocumentVerificationUploadResponse
{
    public string PassportImagePath { get; init; } = string.Empty;
    public string SelfieImagePath { get; init; } = string.Empty;
}
