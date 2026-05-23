using Auctions.Application.Commands.PlaceBid;
using AuHub.Shared.ValueObjects;
using FastEndpoints;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Lots;

public class PlaceBidEndpoint : Endpoint<PlaceBidRequest, PlaceBidResponse>
{
    private readonly PlaceBidCommandHandler _handler;

    public PlaceBidEndpoint(PlaceBidCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/bids");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Place a bid on a lot";
            s.Description = "Place a new bid on an active auction lot. Requires authentication.";
        });
    }

    public override async Task HandleAsync(PlaceBidRequest req, CancellationToken ct)
    {
        if (req.Amount.Amount <= 0)
        {
            ThrowError("Bid amount must be greater than 0", 400);
            return;
        }
        if (req.Amount.Amount > 999999999)
        {
            ThrowError("Bid amount exceeds maximum allowed value", 400);
            return;
        }

        var lotId = Route<Guid>("id");

        var bidderIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(bidderIdClaim) || !Guid.TryParse(bidderIdClaim, out var bidderId))
        {
            ThrowError("Invalid user ID in token", 401);
            return;
        }

        var bidderName = User.FindFirst(ClaimTypes.Name)?.Value
                         ?? User.FindFirst("name")?.Value
                         ?? "Unknown";

        var command = new PlaceBidCommand
        {
            LotId = lotId,
            BidderId = bidderId,
            BidderName = bidderName,
            Amount = req.Amount,
            IdempotencyKey = req.IdempotencyKey
        };

        var result = await _handler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}

public record PlaceBidRequest
{
    public Money Amount { get; init; } = Money.Zero;
    public Guid? IdempotencyKey { get; init; }
}
