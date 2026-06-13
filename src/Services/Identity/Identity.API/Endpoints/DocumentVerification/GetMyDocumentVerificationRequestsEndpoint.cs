using System.Security.Claims;
using FastEndpoints;
using Identity.Domain.Interfaces;

namespace Identity.API.Endpoints.DocumentVerification;

public class GetMyDocumentVerificationRequestsEndpoint : EndpointWithoutRequest<List<DocumentVerificationResponse>>
{
    private readonly IDocumentVerificationRequestRepository _requestRepository;

    public GetMyDocumentVerificationRequestsEndpoint(IDocumentVerificationRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public override void Configure()
    {
        Get("/api/auth/document-verification/my");
        Roles("User");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user token", 401);
            return;
        }

        var requests = await _requestRepository.GetByUserIdAsync(userId, ct);
        Response = requests.Select(request => request.ToResponse()).ToList();
    }
}
