using Auctions.Application.Commands.PlaceBid;
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
        Summary(s =>
        {
            s.Summary = "Place a bid on a lot";
            s.Description = "Place a new bid on an active auction lot. Requires authentication.";
        });
    }

    public override async Task HandleAsync(PlaceBidRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0)
        {
            ThrowError("Bid amount must be greater than 0", 400);
            return;
        }
        if (req.Amount > 999999999)
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
        
        var command = new PlaceBidCommand
        {
            LotId = lotId,
            BidderId = bidderId,
            Amount = req.Amount
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
    public decimal Amount { get; init; }
}
