using FastEndpoints;
using System.Security.Claims;
using Payment.Application.Queries.GetTransactionHistory;

namespace Payment.API.Endpoints.Payment;

public class GetTransactionHistoryEndpoint : EndpointWithoutRequest<TransactionHistoryResponse>
{
    private readonly GetTransactionHistoryQueryHandler _handler;

    public GetTransactionHistoryEndpoint(GetTransactionHistoryQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/payment/transactions");
        Roles("Admin", "User");
        Summary(s =>
        {
            s.Summary = "Get transaction history";
            s.Description = "Returns paginated transaction history for the current user.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID", 401);
            return;
        }

        var page = Query<int>("page", isRequired: false);
        var pageSize = Query<int>("pageSize", isRequired: false);

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = new GetTransactionHistoryQuery
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize
        };

        var result = await _handler.HandleAsync(query, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}
