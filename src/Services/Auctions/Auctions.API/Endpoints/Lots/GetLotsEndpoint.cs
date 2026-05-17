using Auctions.Application.Queries.GetLots;
using Auctions.Application.Services;
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
            s.Description = "Returns a paginated list of auction lots with optional filtering";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var onlyActive = Query<bool>("onlyActive", isRequired: false);
        var page = Query<int>("page", isRequired: false);
        var pageSize = Query<int>("pageSize", isRequired: false);

        if (page < 1)
            page = 1;
        if (pageSize < 1 || pageSize > 100)
            pageSize = 10;

        var query = new GetLotsQuery
        {
            OnlyActive = onlyActive,
            Page = page,
            PageSize = pageSize
        };

        var result = await _handler.HandleAsync(query, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        var lotDtos = new List<LotDto>();
        foreach (var l in result.Value.Lots)
        {
            string? coverImageUrl = null;
            if (!string.IsNullOrEmpty(l.CoverImageUrl))
            {
                var fileName = l.CoverImageUrl.Split('/').Last();
                var baseUrl = "http://localhost:5000";
                coverImageUrl = $"{baseUrl}/api/lots/{l.Id}/images/{fileName}";
            }

            lotDtos.Add(new LotDto
            {
                Id = l.Id,
                Title = l.Title,
                Description = l.Description,
                StartingPrice = l.StartingPrice,
                CurrentPrice = l.CurrentPrice,
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                Status = l.Status,
                BidsCount = l.BidsCount,
                CoverImageUrl = coverImageUrl
            });
        }

        Response = new GetLotsResponse
        {
            Success = true,
            Lots = lotDtos,
            Page = result.Value.Page,
            PageSize = result.Value.PageSize,
            TotalCount = result.Value.TotalCount,
            TotalPages = result.Value.TotalPages
        };
    }
}

public record GetLotsResponse
{
    public bool Success { get; init; }
    public List<LotDto> Lots { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
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
    public string? CoverImageUrl { get; init; }
}
