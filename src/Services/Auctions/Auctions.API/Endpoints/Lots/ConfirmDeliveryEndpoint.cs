using FastEndpoints;
using System.Security.Claims;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;

namespace Auctions.API.Endpoints.Lots;

public class ConfirmDeliveryEndpoint : EndpointWithoutRequest
{
    private readonly ILotRepository _lotRepository;
    private readonly AuctionSettlementService _settlementService;
    private readonly TrustScoreService _trustScoreService;

    public ConfirmDeliveryEndpoint(
        ILotRepository lotRepository,
        AuctionSettlementService settlementService,
        TrustScoreService trustScoreService)
    {
        _lotRepository = lotRepository;
        _settlementService = settlementService;
        _trustScoreService = trustScoreService;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/confirm-delivery");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Confirm delivery (Buyer only)";
            s.Description = "Confirm that the lot has been received.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID", 401);
            return;
        }

        var lot = await _lotRepository.GetByIdAsync(lotId, ct);
        if (lot == null)
        {
            ThrowError("Lot not found", 404);
            return;
        }

        if (lot.WinnerId != userId)
        {
            ThrowError("Only winner can confirm delivery", 403);
            return;
        }

        lot.ConfirmDelivery();

        var payoutResult = await _settlementService.PaySellerAsync(lot, ct);
        if (payoutResult.IsFailure)
        {
            ThrowError(payoutResult.Error, payoutResult.StatusCode);
            return;
        }

        lot.CompleteTransaction();
        await _lotRepository.SaveChangesAsync(ct);
        await _trustScoreService.RecordSuccessfulSaleAsync(lot, ct);

        Response = new
        {
            Success = true,
            Message = "Delivery confirmed",
            SellerPayout = payoutResult.Value.Amount
        };
    }
}
