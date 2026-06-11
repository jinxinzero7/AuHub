using FastEndpoints;
using System.Security.Claims;
using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;

namespace Auctions.API.Endpoints.Lots;

public class UnfreezeLotEndpoint : EndpointWithoutRequest
{
    private readonly ILotRepository _lotRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;

    public UnfreezeLotEndpoint(ILotRepository lotRepository, IAdminAuditLogRepository auditLogRepository)
    {
        _lotRepository = lotRepository;
        _auditLogRepository = auditLogRepository;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/unfreeze");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Unfreeze a lot (Admin only)";
            s.Description = "Unfreeze a frozen lot and return it to Active status.";
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

        lot.Unfreeze();
        await _lotRepository.SaveChangesAsync(ct);
        await _auditLogRepository.AddAsync(AdminAuditLog.Create(GetActorUserId(), "LotUnfreeze", "Lot", lot.Id, null), ct);
        await _auditLogRepository.SaveChangesAsync(ct);

        Response = new { Success = true, Message = "Lot unfrozen" };
    }

    private Guid? GetActorUserId()
    {
        var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(actorIdClaim, out var actorId) ? actorId : null;
    }
}
