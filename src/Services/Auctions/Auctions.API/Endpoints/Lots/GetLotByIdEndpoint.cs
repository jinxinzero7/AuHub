using Auctions.Application.Queries.GetLotById;
using FastEndpoints;

namespace Auctions.API.Endpoints.Lots;

public class GetLotByIdEndpoint : EndpointWithoutRequest<LotDetailResponse>
{
    private readonly GetLotByIdQueryHandler _handler;

    public GetLotByIdEndpoint(GetLotByIdQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/lots/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");
        var query = new GetLotByIdQuery { LotId = lotId };

        var result = await _handler.HandleAsync(query, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}
