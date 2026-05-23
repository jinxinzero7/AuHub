using FastEndpoints;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using Auctions.Domain.Enums;

namespace Auctions.API.Endpoints.Lots;

public class FreezeLotEndpoint : EndpointWithoutRequest
{
    private readonly ILotRepository _lotRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly INotificationClient _notificationClient;

    public FreezeLotEndpoint(ILotRepository lotRepository, IEventPublisher eventPublisher, INotificationClient notificationClient)
    {
        _lotRepository = lotRepository;
        _eventPublisher = eventPublisher;
        _notificationClient = notificationClient;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/freeze");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Freeze a lot (Admin only)";
            s.Description = "Freeze an active lot for investigation.";
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

        lot.Freeze();
        await _lotRepository.SaveChangesAsync(ct);

        await _notificationClient.SendNotificationAsync(lot.SellerId, NotificationType.LotFrozen, "Лот заморожен", $"Ваш лот «{lot.Title}» заморожен администратором", ct);
        if (lot.WinnerId.HasValue)
            await _notificationClient.SendNotificationAsync(lot.WinnerId.Value, NotificationType.LotFrozen, "Лот заморожен", $"Лот «{lot.Title}», на который вы ставили, заморожен", ct);

        await _eventPublisher.PublishUserNotificationAsync(lot.SellerId, "LotFrozen", $"Ваш лот «{lot.Title}» заморожен", lot.Id, ct);

        Response = new { Success = true, Message = "Lot frozen" };
    }
}
