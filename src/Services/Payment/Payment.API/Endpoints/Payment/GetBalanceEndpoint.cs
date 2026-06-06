using FastEndpoints;
using System.Security.Claims;
using Payment.Application.Queries.GetBalance;

namespace Payment.API.Endpoints.Payment;

public class GetBalanceEndpoint : EndpointWithoutRequest<BalanceResponse>
{
    private readonly GetBalanceQueryHandler _handler;

    public GetBalanceEndpoint(GetBalanceQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/payment/balance");
        Roles("Admin", "User");
        Summary(s =>
        {
            s.Summary = "Get wallet balance";
            s.Description = "Returns current balance and frozen balance.";
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

        var query = new GetBalanceQuery { UserId = userId };
        var result = await _handler.HandleAsync(query, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        Response = result.Value;
    }
}
