using Auctions.Application.Queries.GetLots;
using Auctions.Application.Options;
using AuHub.Shared.ValueObjects;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace Auctions.API.Endpoints.Lots;

public class GetLotsEndpoint : EndpointWithoutRequest<GetLotsResponse>
{
    private readonly GetLotsQueryHandler _handler;
    private readonly ExternalUrlOptions _externalUrl;

    public GetLotsEndpoint(GetLotsQueryHandler handler, IOptions<ExternalUrlOptions> externalUrl)
    {
        _handler = handler;
        _externalUrl = externalUrl.Value;
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
        var search = Query<string>("search", isRequired: false);

        if (page < 1)
            page = 1;
        if (pageSize < 1 || pageSize > 100)
            pageSize = 10;

        var query = new GetLotsQuery
        {
            OnlyActive = onlyActive,
            Page = page,
            PageSize = pageSize,
            Search = search
        };

        var result = await _handler.HandleAsync(query, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        var lotDtos = PublicLotDtoMapper.Map(result.Value.Lots, _externalUrl.BaseUrl);

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
    public Money StartingPrice { get; init; } = Money.Zero;
    public Money CurrentPrice { get; init; } = Money.Zero;
    public int DurationHours { get; init; }
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid SellerId { get; init; }
    public int BidsCount { get; init; }
    public string? CoverImageUrl { get; init; }
    public List<string> SupportedDeliveryProviders { get; init; } = new();
}
