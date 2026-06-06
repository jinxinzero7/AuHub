using AuHub.Shared.Security;
using FastEndpoints;
using Payment.Application.Queries.GetBalance;

namespace Payment.API.Endpoints.Payment;

public class GetInternalBalanceEndpoint : EndpointWithoutRequest<BalanceResponse>
{
    private readonly GetBalanceQueryHandler _handler;

    public GetInternalBalanceEndpoint(GetBalanceQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/payment/internal/balance");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get wallet balance (internal)";
            s.Description = "Returns user balance for internal service-to-service checks.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!InternalApiKey.IsValid(HttpContext))
        {
            ThrowError("Unauthorized: invalid or missing internal API key", 401);
            return;
        }

        var userIdStr = Query<string>("userId", isRequired: true);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            ThrowError("Invalid user ID", 400);
            return;
        }

        var result = await _handler.HandleAsync(new GetBalanceQuery { UserId = userId }, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        Response = result.Value;
    }
}
