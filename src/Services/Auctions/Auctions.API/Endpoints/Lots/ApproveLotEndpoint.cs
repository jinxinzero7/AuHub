using FastEndpoints;
using System.Security.Claims;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;

namespace Auctions.API.Endpoints.Lots;

public class ApproveLotEndpoint : EndpointWithoutRequest
{
    private readonly ILotRepository _lotRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly INotificationClient _notificationClient;
    private readonly IAdminAuditLogRepository _auditLogRepository;

    public ApproveLotEndpoint(
        ILotRepository lotRepository,
        IEventPublisher eventPublisher,
        INotificationClient notificationClient,
        IAdminAuditLogRepository auditLogRepository)
    {
        _lotRepository = lotRepository;
        _eventPublisher = eventPublisher;
        _notificationClient = notificationClient;
        _auditLogRepository = auditLogRepository;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/approve");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Approve a lot (Admin only)";
            s.Description = "Change lot status from PendingModeration to Active after moderation.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        var lot = await _lotRepository.GetByIdAsync(lotId, ct);
        if (lot == null)
        {
            ThrowError("Lot not found", 404);
            return;
        }

        try
        {
            lot.Approve();
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, 400);
            return;
        }

        await _lotRepository.SaveChangesAsync(ct);
        await _auditLogRepository.AddAsync(AdminAuditLog.Create(GetActorUserId(), "LotApprove", "Lot", lot.Id, null), ct);
        await _auditLogRepository.SaveChangesAsync(ct);

        await _notificationClient.SendNotificationAsync(lot.SellerId, NotificationType.LotApproved, "Лот одобрен", $"Ваш лот «{lot.Title}» прошёл модерацию", ct);
        await _eventPublisher.PublishUserNotificationAsync(lot.SellerId, "LotApproved", $"Ваш лот «{lot.Title}» одобрен", lot.Id, ct);

        Response = new { Success = true, Message = "Lot approved" };
    }

    private Guid? GetActorUserId()
    {
        var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(actorIdClaim, out var actorId) ? actorId : null;
    }
}
