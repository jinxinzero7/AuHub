using Auctions.Application.Queries.GetLots;
using FastEndpoints;

namespace Auctions.API.Endpoints.Lots;

public class GetLotsEndpoint : EndpointWithoutRequest<GetLotsResponse>
{
    private readonly GetLotsQueryHandler _handler;

    public GetLotsEndpoint(GetLotsQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/lots");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get all auction lots";
            s.Description = "Returns a list of all auction lots with optional filtering";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var onlyActive = Query<bool>("onlyActive", false);
        
        var query = new GetLotsQuery
        {
            OnlyActive = onlyActive
        };

        var result = await _handler.HandleAsync(query, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = new GetLotsResponse
        {
            Success = true,
            Lots = result.Value.Select(l => new LotDto
            {
                Id = l.Id,
                Title = l.Title,
                Description = l.Description,
                StartingPrice = l.StartingPrice,
                CurrentPrice = l.CurrentPrice,
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                Status = l.Status,
                BidsCount = l.BidsCount
            }).ToList()
        };
    }
}

public record GetLotsResponse
{
    public bool Success { get; init; }
    public List<LotDto> Lots { get; init; } = new();
    public string? Error { get; init; }
}

public record LotDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal StartingPrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public int BidsCount { get; init; }
}
