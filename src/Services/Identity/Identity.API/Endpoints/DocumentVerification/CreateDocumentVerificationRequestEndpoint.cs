using System.Security.Claims;
using FastEndpoints;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;

namespace Identity.API.Endpoints.DocumentVerification;

public class CreateDocumentVerificationRequestEndpoint : Endpoint<CreateDocumentVerificationRequest, DocumentVerificationResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDocumentVerificationRequestRepository _requestRepository;

    public CreateDocumentVerificationRequestEndpoint(
        IUserRepository userRepository,
        IDocumentVerificationRequestRepository requestRepository)
    {
        _userRepository = userRepository;
        _requestRepository = requestRepository;
    }

    public override void Configure()
    {
        Post("/api/auth/document-verification/request");
        Roles("User");
    }

    public override async Task HandleAsync(CreateDocumentVerificationRequest req, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            ThrowError("Invalid user token", 401);
            return;
        }

        var user = await _userRepository.GetByIdAsync(userId.Value, ct);
        if (user == null)
        {
            ThrowError("User not found", 404);
            return;
        }

        if (user.DocumentVerificationStatus == UserDocumentVerificationStatus.Verified)
        {
            ThrowError("User is already document verified", 400);
            return;
        }

        var pendingRequest = await _requestRepository.GetPendingByUserIdAsync(user.Id, ct);
        if (pendingRequest != null)
        {
            ThrowError("Document verification request is already pending", 409);
            return;
        }

        try
        {
            var request = DocumentVerificationRequest.Create(user.Id, req.PassportImagePath, req.SelfieImagePath);
            user.MarkDocumentVerificationPending();

            await _requestRepository.AddAsync(request, ct);
            await _requestRepository.SaveChangesAsync(ct);
            await _userRepository.SaveChangesAsync(ct);

            Response = request.ToResponse();
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, 400);
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public record CreateDocumentVerificationRequest
{
    public string PassportImagePath { get; init; } = string.Empty;
    public string SelfieImagePath { get; init; } = string.Empty;
}
