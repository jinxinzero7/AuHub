using Auctions.Application.Commands.CompleteLot;
using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;
using FastEndpoints;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Lots;

public class CompleteLotEndpoint : EndpointWithoutRequest<CompleteLotResponse>
{
    private readonly CompleteLotCommandHandler _handler;
    private readonly IAdminAuditLogRepository _auditLogRepository;

    public CompleteLotEndpoint(CompleteLotCommandHandler handler, IAdminAuditLogRepository auditLogRepository)
    {
        _handler = handler;
        _auditLogRepository = auditLogRepository;
    }

    public override void Configure()
    {
        Post("/api/admin/lots/{id}/force-complete");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Force-complete a lot (Admin only)";
            s.Description = "Administrative fallback to complete an active auction lot manually.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        var command = new CompleteLotCommand
        {
            LotId = lotId
        };

        var result = await _handler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        await _auditLogRepository.AddAsync(AdminAuditLog.Create(GetActorUserId(), "LotForceComplete", "Lot", lotId, null), ct);
        await _auditLogRepository.SaveChangesAsync(ct);

        Response = result.Value;
    }

    private Guid? GetActorUserId()
    {
        var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(actorIdClaim, out var actorId) ? actorId : null;
    }
}
