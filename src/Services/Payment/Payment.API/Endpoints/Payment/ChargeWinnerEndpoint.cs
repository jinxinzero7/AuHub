using AuHub.Shared.ValueObjects;
using FastEndpoints;
using Payment.Application.Commands.ChargeWinner;

namespace Payment.API.Endpoints.Payment;

public class ChargeWinnerEndpoint : Endpoint<ChargeWinnerRequest, PaymentOperationResponse>
{
    private readonly ChargeWinnerCommandHandler _handler;

    public ChargeWinnerEndpoint(ChargeWinnerCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/payment/charge-winner");
        AllowAnonymous(); // Internal service-to-service call
        Summary(s =>
        {
            s.Summary = "Charge winner after auction completion";
            s.Description = "Withdraw funds from winner's wallet (internal).";
        });
    }

    public override async Task HandleAsync(ChargeWinnerRequest req, CancellationToken ct)
    {
        if (req.Amount.Amount <= 0)
        {
            ThrowError("Amount must be greater than 0", 400);
            return;
        }

        var command = new ChargeWinnerCommand
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

        Response = new PaymentOperationResponse(true, "Winner charged");
    }
}

public record ChargeWinnerRequest
{
    public Guid UserId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Guid LotId { get; init; }
}
