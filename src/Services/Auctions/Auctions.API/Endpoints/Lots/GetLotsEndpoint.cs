using Auctions.Application.Queries.GetLots;
using Auctions.Application.Services;
using AuHub.Shared.ValueObjects;
using FastEndpoints;

namespace Auctions.API.Endpoints.Lots;

public class GetLotsEndpoint : EndpointWithoutRequest<GetLotsResponse>
{
    private readonly GetLotsQueryHandler _handler;
    private readonly IImageStorageService _storageService;

    public GetLotsEndpoint(GetLotsQueryHandler handler, IImageStorageService storageService)
    {
        _handler = handler;
        _storageService = storageService;
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
        var sellerId = Query<string>("sellerId", isRequired: false);
        var winnerId = Query<string>("winnerId", isRequired: false);
        var includeDrafts = Query<bool>("includeDrafts", isRequired: false);
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
            SellerId = sellerId,
            WinnerId = winnerId,
            IncludeDrafts = includeDrafts,
            Search = search
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
                coverImageUrl = await _storageService.GetPresignedUrlAsync($"lots/{l.Id}/{fileName}", 1440, ct);
            }

            lotDtos.Add(new LotDto
            {
                Id = l.Id,
                Title = l.Title,
                Description = l.Description,
                StartingPrice = l.StartingPrice,
                CurrentPrice = l.CurrentPrice,
                DurationHours = l.DurationHours,
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                Status = l.Status,
                SellerId = l.SellerId,
                WinnerId = l.WinnerId,
                BidsCount = l.BidsCount,
                CoverImageUrl = coverImageUrl,
                TrackingNumber = l.TrackingNumber,
                SelectedDeliveryProvider = l.SelectedDeliveryProvider,
                DeliveryRequestedAt = l.DeliveryRequestedAt,
                DeliveryRequestDeadlineAt = l.DeliveryRequestDeadlineAt,
                SupportedDeliveryProviders = l.SupportedDeliveryProviders,
                AdminComment = l.AdminComment
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
    public Money StartingPrice { get; init; } = Money.Zero;
    public Money CurrentPrice { get; init; } = Money.Zero;
    public int DurationHours { get; init; }
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid SellerId { get; init; }
    public Guid? WinnerId { get; init; }
    public int BidsCount { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? TrackingNumber { get; init; }
    public string? DeliveryAddress { get; init; }
    public string? SelectedDeliveryProvider { get; init; }
    public DateTime? DeliveryRequestedAt { get; init; }
    public DateTime? DeliveryRequestDeadlineAt { get; init; }
    public List<string> SupportedDeliveryProviders { get; init; } = new();
    public string? AdminComment { get; init; }
}
