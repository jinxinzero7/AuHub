using FastEndpoints;
using Identity.Application.Services;
using Identity.Domain.Interfaces;

namespace Identity.API.Endpoints.DocumentVerification;

public class GetDocumentVerificationFileEndpoint : EndpointWithoutRequest
{
    private readonly IDocumentVerificationRequestRepository _requestRepository;
    private readonly IDocumentStorageService _documentStorageService;

    public GetDocumentVerificationFileEndpoint(
        IDocumentVerificationRequestRepository requestRepository,
        IDocumentStorageService documentStorageService)
    {
        _requestRepository = requestRepository;
        _documentStorageService = documentStorageService;
    }

    public override void Configure()
    {
        Get("/api/auth/document-verification/{id}/files/{fileType}");
        Roles("Admin");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var request = await _requestRepository.GetByIdAsync(Route<Guid>("id"), ct);
        if (request == null)
        {
            ThrowError("Document verification request not found", 404);
            return;
        }

        var fileType = (Route<string>("fileType") ?? string.Empty).Trim().ToLowerInvariant();
        var objectName = fileType switch
        {
            "passport" => request.PassportImagePath,
            "selfie" => request.SelfieImagePath,
            _ => null
        };

        if (objectName == null)
        {
            ThrowError("Unknown document file type", 400);
            return;
        }

        try
        {
            var file = await _documentStorageService.GetStreamAsync(objectName, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            HttpContext.Response.ContentType = file.ContentType;
            HttpContext.Response.ContentLength = file.Size;
            HttpContext.Response.Headers.ContentDisposition = $"inline; filename=\"{request.Id}-{fileType}{GetExtension(file.ContentType)}\"";
            await file.Stream.CopyToAsync(HttpContext.Response.Body, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            ThrowError("Document file not found", 404);
        }
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
