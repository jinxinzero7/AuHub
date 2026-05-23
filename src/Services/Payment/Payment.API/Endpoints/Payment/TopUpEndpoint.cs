using AuHub.Shared.ValueObjects;
using FastEndpoints;
using System.Security.Claims;
using Payment.Application.Commands.TopUpWallet;

namespace Payment.API.Endpoints.Payment;

public class TopUpEndpoint : Endpoint<TopUpRequest>
{
    private readonly TopUpWalletCommandHandler _handler;

    public TopUpEndpoint(TopUpWalletCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/payment/topup");
        Roles("Admin", "User");
        Summary(s =>
        {
            s.Summary = "Top up wallet balance";
            s.Description = "Add funds to user wallet (demo mode).";
        });
    }

    public override async Task HandleAsync(TopUpRequest req, CancellationToken ct)
    {
        if (req.Amount.Amount <= 0)
        {
            ThrowError("Amount must be greater than 0", 400);
            return;
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID", 401);
            return;
        }

        var command = new TopUpWalletCommand
        {
            UserId = userId,
            Amount = req.Amount
        };

        var result = await _handler.HandleAsync(command, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        await SendOkAsync(ct);
    }
}

public record TopUpRequest
{
    public Money Amount { get; init; } = Money.Zero;
}
