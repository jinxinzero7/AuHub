using AuHub.Shared.ValueObjects;
using AuHub.Shared.Security;
using FastEndpoints;
using Payment.Application.Commands.ReleaseFunds;

namespace Payment.API.Endpoints.Payment;

public class ReleaseFundsEndpoint : Endpoint<ReleaseFundsRequest, PaymentOperationResponse>
{
    private readonly ReleaseFundsCommandHandler _handler;

    public ReleaseFundsEndpoint(ReleaseFundsCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/payment/release");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Release reserved funds";
            s.Description = "Unfreeze funds when bid is outbid (internal).";
        });
    }

    public override async Task HandleAsync(ReleaseFundsRequest req, CancellationToken ct)
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

        var command = new ReleaseFundsCommand
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

        Response = new PaymentOperationResponse(true, "Funds released");
    }
}

public record ReleaseFundsRequest
{
    public Guid UserId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Guid LotId { get; init; }
}
