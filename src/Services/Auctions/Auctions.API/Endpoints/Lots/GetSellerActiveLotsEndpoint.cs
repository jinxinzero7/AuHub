using Auctions.Application.Queries.GetLots;
using Auctions.Application.Services;
using FastEndpoints;

namespace Auctions.API.Endpoints.Lots;

public class GetSellerActiveLotsEndpoint : EndpointWithoutRequest<GetLotsResponse>
{
    private const int DefaultPageSize = 9;
    private const int MaxPageSize = 100;

    private readonly GetLotsQueryHandler _handler;
    private readonly IImageStorageService _storageService;

    public GetSellerActiveLotsEndpoint(
        GetLotsQueryHandler handler,
        IImageStorageService storageService)
    {
        _handler = handler;
        _storageService = storageService;
    }

    public override void Configure()
    {
        Get("/api/sellers/{sellerId}/lots");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get active seller lots";
            s.Description = "Returns a public-safe paginated list of active lots for a seller.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sellerId = Route<Guid>("sellerId");
        var page = Query<int>("page", isRequired: false);
        var pageSize = Query<int>("pageSize", isRequired: false);

        if (page < 1)
            page = 1;
        if (pageSize < 1 || pageSize > MaxPageSize)
            pageSize = DefaultPageSize;

        var result = await _handler.HandleAsync(new GetLotsQuery
        {
            SellerId = sellerId.ToString(),
            OnlyActive = true,
            Page = page,
            PageSize = pageSize
        }, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = new GetLotsResponse
        {
            Success = true,
            Lots = await PublicLotDtoMapper.MapAsync(result.Value.Lots, _storageService, ct),
            Page = result.Value.Page,
            PageSize = result.Value.PageSize,
            TotalCount = result.Value.TotalCount,
            TotalPages = result.Value.TotalPages
        };
    }
}
