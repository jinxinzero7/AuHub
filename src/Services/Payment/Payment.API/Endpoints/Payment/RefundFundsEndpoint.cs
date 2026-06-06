using AuHub.Shared.ValueObjects;
using FastEndpoints;
using Payment.Application.Commands.RefundFunds;

namespace Payment.API.Endpoints.Payment;

public class RefundFundsEndpoint : Endpoint<RefundFundsRequest, PaymentOperationResponse>
{
    private readonly RefundFundsCommandHandler _handler;

    public RefundFundsEndpoint(RefundFundsCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/payment/refund");
        AllowAnonymous(); // Internal service-to-service call
        Summary(s =>
        {
            s.Summary = "Refund funds to user";
            s.Description = "Return funds in case of dispute or cancellation (internal).";
        });
    }

    public override async Task HandleAsync(RefundFundsRequest req, CancellationToken ct)
    {
        if (req.Amount.Amount <= 0)
        {
            ThrowError("Amount must be greater than 0", 400);
            return;
        }

        var command = new RefundFundsCommand
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

        Response = new PaymentOperationResponse(true, "Funds refunded");
    }
}

public record RefundFundsRequest
{
    public Guid UserId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Guid LotId { get; init; }
}
