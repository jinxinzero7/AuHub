using AuHub.Shared.ValueObjects;
using AuHub.Shared.Security;
using FastEndpoints;
using Payment.Application.Commands.ReserveFunds;

namespace Payment.API.Endpoints.Payment;

public class ReserveFundsEndpoint : Endpoint<ReserveFundsRequest, PaymentOperationResponse>
{
    private readonly ReserveFundsCommandHandler _handler;

    public ReserveFundsEndpoint(ReserveFundsCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/payment/reserve");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Reserve funds for bid";
            s.Description = "Freeze funds when user places a bid (internal).";
        });
    }

    public override async Task HandleAsync(ReserveFundsRequest req, CancellationToken ct)
    {
        if (!InternalApiKey.IsValid(HttpContext))
        {
            ThrowError("Unauthorized: invalid or missing internal API key", 401);
            return;
        }

        if (req.Amount.Amount <= 0)
        {
            ThrowError("Amount must be greater than 0", 400);
            return;
        }

        var command = new ReserveFundsCommand
        {
            UserId = req.UserId,
            Amount = req.Amount,
            ReferenceId = req.LotId
        };

        var result = await _handler.HandleAsync(command, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        Response = new PaymentOperationResponse(true, "Funds reserved");
    }
}

public record ReserveFundsRequest
{
    public Guid UserId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Guid LotId { get; init; }
}
