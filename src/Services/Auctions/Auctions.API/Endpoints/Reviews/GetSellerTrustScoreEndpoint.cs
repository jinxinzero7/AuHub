using Auctions.Application.Services;
using FastEndpoints;

namespace Auctions.API.Endpoints.Reviews;

public class GetSellerTrustScoreEndpoint : EndpointWithoutRequest<SellerTrustScoreResponse>
{
    private readonly TrustScoreService _trustScoreService;

    public GetSellerTrustScoreEndpoint(TrustScoreService trustScoreService)
    {
        _trustScoreService = trustScoreService;
    }

    public override void Configure()
    {
        Get("/api/sellers/{sellerId}/trust");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get public seller trust score";
            s.Description = "Returns a public reliability score and badge based on event-like trust score records.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Response = await _trustScoreService.GetSellerTrustScoreAsync(Route<Guid>("sellerId"), ct);
    }
}
