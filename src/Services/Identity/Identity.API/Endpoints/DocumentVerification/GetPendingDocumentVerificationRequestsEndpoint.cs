using FastEndpoints;
using Identity.Domain.Interfaces;

namespace Identity.API.Endpoints.DocumentVerification;

public class GetPendingDocumentVerificationRequestsEndpoint : EndpointWithoutRequest<List<DocumentVerificationResponse>>
{
    private readonly IDocumentVerificationRequestRepository _requestRepository;

    public GetPendingDocumentVerificationRequestsEndpoint(IDocumentVerificationRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public override void Configure()
    {
        Get("/api/auth/document-verification/pending");
        Roles("Admin");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var requests = await _requestRepository.GetPendingAsync(ct);
        Response = requests.Select(request => request.ToResponse()).ToList();
    }
}
