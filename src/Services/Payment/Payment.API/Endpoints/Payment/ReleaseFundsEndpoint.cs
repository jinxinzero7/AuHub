using AuHub.Shared.ValueObjects;
using FastEndpoints;
using Payment.Application.Commands.ReleaseFunds;

namespace Payment.API.Endpoints.Payment;

public class ReleaseFundsEndpoint : Endpoint<ReleaseFundsRequest>
{
    private readonly ReleaseFundsCommandHandler _handler;

    public ReleaseFundsEndpoint(ReleaseFundsCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/payment/release");
        AllowAnonymous(); // Internal service-to-service call
        Summary(s =>
        {
            s.Summary = "Release reserved funds";
            s.Description = "Unfreeze funds when bid is outbid (internal).";
        });
    }

    public override async Task HandleAsync(ReleaseFundsRequest req, CancellationToken ct)
    {
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

        await SendOkAsync(new { Success = true, Message = "Funds released" }, ct);
    }
}

public record ReleaseFundsRequest
{
    public Guid UserId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Guid LotId { get; init; }
}
