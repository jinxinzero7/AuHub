using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using Auctions.Domain.Enums;

namespace Auctions.API.Endpoints.Lots;

public class RejectLotEndpoint : Endpoint<RejectLotRequest>
{
    private readonly ILotRepository _lotRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly INotificationClient _notificationClient;

    public RejectLotEndpoint(ILotRepository lotRepository, IEventPublisher eventPublisher, INotificationClient notificationClient)
    {
        _lotRepository = lotRepository;
        _eventPublisher = eventPublisher;
        _notificationClient = notificationClient;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/reject");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Reject a lot (Admin only)";
            s.Description = "Change lot status from PendingModeration to Rejected with a reason.";
        });
    }

    public override async Task HandleAsync(RejectLotRequest req, CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        if (string.IsNullOrWhiteSpace(req.Reason))
        {
            ThrowError("Rejection reason is required", 400);
            return;
        }

        var lot = await _lotRepository.GetByIdAsync(lotId, ct);
        if (lot == null)
        {
            ThrowError("Lot not found", 404);
            return;
        }

        try
        {
            lot.Reject(req.Reason);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, 400);
            return;
        }

        await _lotRepository.SaveChangesAsync(ct);

        await _notificationClient.SendNotificationAsync(lot.SellerId, NotificationType.LotRejected, "Лот отклонён", $"Ваш лот «{lot.Title}» отклонён. Причина: {req.Reason}", ct);
        await _eventPublisher.PublishUserNotificationAsync(lot.SellerId, "LotRejected", $"Ваш лот «{lot.Title}» отклонён: {req.Reason}", lot.Id, ct);

        Response = new { Success = true, Message = "Lot rejected" };
    }
}

public record RejectLotRequest
{
    public string Reason { get; init; } = string.Empty;
}
