using FastEndpoints;
using System.Security.Claims;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;

namespace Identity.API.Endpoints.Users;

public class UnbanUserEndpoint : EndpointWithoutRequest
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;

    public UnbanUserEndpoint(IUserRepository userRepository, IAdminAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
    }

    public override void Configure()
    {
        Post("/api/auth/users/{id}/unban");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Unban a user (Admin only)";
            s.Description = "Remove ban from a user.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Route<Guid>("id");

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            ThrowError("User not found", 404);
            return;
        }

        user.Unban();
        await _auditLogRepository.AddAsync(AdminAuditLog.Create(GetActorUserId(), "UserUnban", "User", user.Id, null), ct);
        await _userRepository.SaveChangesAsync(ct);
        await _auditLogRepository.SaveChangesAsync(ct);

        await HttpContext.Response.WriteAsJsonAsync(new { success = true }, ct);
    }

    private Guid? GetActorUserId()
    {
        var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(actorIdClaim, out var actorId) ? actorId : null;
    }
}
