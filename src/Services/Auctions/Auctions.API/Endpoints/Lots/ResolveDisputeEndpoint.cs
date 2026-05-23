using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using Auctions.Domain.Enums;

namespace Auctions.API.Endpoints.Lots;

public class ResolveDisputeEndpoint : Endpoint<ResolveDisputeRequest>
{
    private readonly ILotRepository _lotRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly INotificationClient _notificationClient;

    public ResolveDisputeEndpoint(ILotRepository lotRepository, IEventPublisher eventPublisher, INotificationClient notificationClient)
    {
        _lotRepository = lotRepository;
        _eventPublisher = eventPublisher;
        _notificationClient = notificationClient;
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

        lot.ResolveDispute(req.InFavorOfBuyer);
        await _lotRepository.SaveChangesAsync(ct);

        var resolution = req.InFavorOfBuyer ? "в пользу покупателя" : "в пользу продавца";
        await _notificationClient.SendNotificationAsync(lot.SellerId, NotificationType.DisputeResolved, "Спор разрешён", $"Спор по лоту «{lot.Title}» разрешён {resolution}", ct);
        if (lot.WinnerId.HasValue)
            await _notificationClient.SendNotificationAsync(lot.WinnerId.Value, NotificationType.DisputeResolved, "Спор разрешён", $"Спор по лоту «{lot.Title}» разрешён {resolution}", ct);

        await _eventPublisher.PublishUserNotificationAsync(lot.SellerId, "DisputeResolved", $"Спор по лоту «{lot.Title}» разрешён {resolution}", lot.Id, ct);

        Response = new { Success = true, Message = "Dispute resolved" };
    }
}

public record ResolveDisputeRequest
{
    public bool InFavorOfBuyer { get; init; }
}
