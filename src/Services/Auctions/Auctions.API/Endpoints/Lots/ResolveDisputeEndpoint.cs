using FastEndpoints;
using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;

namespace Auctions.API.Endpoints.Lots;

public class ResolveDisputeEndpoint : Endpoint<ResolveDisputeRequest>
{
    private readonly ILotRepository _lotRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly INotificationClient _notificationClient;
    private readonly AuctionSettlementService _settlementService;

    public ResolveDisputeEndpoint(
        ILotRepository lotRepository,
        IEventPublisher eventPublisher,
        INotificationClient notificationClient,
        AuctionSettlementService settlementService)
    {
        _lotRepository = lotRepository;
        _eventPublisher = eventPublisher;
        _notificationClient = notificationClient;
        _settlementService = settlementService;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/resolve-dispute");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Resolve a dispute (Admin only)";
            s.Description = "Resolve a dispute in favor of buyer or seller.";
        });
    }

    public override async Task HandleAsync(ResolveDisputeRequest req, CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        var lot = await _lotRepository.GetByIdAsync(lotId, ct);
        if (lot == null)
        {
            ThrowError("Lot not found", 404);
            return;
        }

        if (lot.Status != LotStatus.Disputed)
        {
            ThrowError("Lot is not in dispute", 400);
            return;
        }

        Result<bool>? buyerRefundResult = null;
        Result<Money>? sellerPayoutResult = null;
        if (req.InFavorOfBuyer)
        {
            buyerRefundResult = await _settlementService.RefundWinnerAsync(lot, ct);
            if (buyerRefundResult.IsFailure)
            {
                ThrowError(buyerRefundResult.Error, buyerRefundResult.StatusCode);
                return;
            }
        }
        else
        {
            sellerPayoutResult = await _settlementService.PaySellerAsync(lot, ct);
            if (sellerPayoutResult.IsFailure)
            {
                ThrowError(sellerPayoutResult.Error, sellerPayoutResult.StatusCode);
                return;
            }
        }

        lot.ResolveDispute(req.InFavorOfBuyer);
        await _lotRepository.SaveChangesAsync(ct);

        var resolution = req.InFavorOfBuyer ? "в пользу покупателя" : "в пользу продавца";
        await _notificationClient.SendNotificationAsync(lot.SellerId, NotificationType.DisputeResolved, "Спор разрешён", $"Спор по лоту «{lot.Title}» разрешён {resolution}", ct);
        if (lot.WinnerId.HasValue)
            await _notificationClient.SendNotificationAsync(lot.WinnerId.Value, NotificationType.DisputeResolved, "Спор разрешён", $"Спор по лоту «{lot.Title}» разрешён {resolution}", ct);

        await _eventPublisher.PublishUserNotificationAsync(lot.SellerId, "DisputeResolved", $"Спор по лоту «{lot.Title}» разрешён {resolution}", lot.Id, ct);

        Response = new
        {
            Success = true,
            Message = "Dispute resolved",
            BuyerRefunded = buyerRefundResult?.Value,
            SellerPayout = sellerPayoutResult?.Value.Amount
        };
    }
}

public record ResolveDisputeRequest
{
    public bool InFavorOfBuyer { get; init; }
}
