using System.Security.Claims;
using FastEndpoints;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;

namespace Identity.API.Endpoints.DocumentVerification;

public class RejectDocumentVerificationRequestEndpoint : Endpoint<RejectDocumentVerificationRequest, DocumentVerificationResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDocumentVerificationRequestRepository _requestRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;

    public RejectDocumentVerificationRequestEndpoint(
        IUserRepository userRepository,
        IDocumentVerificationRequestRepository requestRepository,
        IAdminAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _requestRepository = requestRepository;
        _auditLogRepository = auditLogRepository;
    }

    public override void Configure()
    {
        Post("/api/auth/document-verification/{id}/reject");
        Roles("Admin");
    }

    public override async Task HandleAsync(RejectDocumentVerificationRequest req, CancellationToken ct)
    {
        var adminId = GetActorUserId();
        if (adminId == null)
        {
            ThrowError("Invalid admin token", 401);
            return;
        }

        var request = await _requestRepository.GetByIdAsync(Route<Guid>("id"), ct);
        if (request == null)
        {
            ThrowError("Document verification request not found", 404);
            return;
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            ThrowError("User not found", 404);
            return;
        }

        try
        {
            request.Reject(adminId.Value, req.Reason);
            user.MarkDocumentUnverified();

            await _auditLogRepository.AddAsync(AdminAuditLog.Create(adminId, "DocumentVerificationReject", "DocumentVerificationRequest", request.Id, req.Reason), ct);
            await _requestRepository.SaveChangesAsync(ct);
            await _userRepository.SaveChangesAsync(ct);
            await _auditLogRepository.SaveChangesAsync(ct);

            Response = request.ToResponse();
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, 400);
        }
    }

    private Guid? GetActorUserId()
    {
        var actorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(actorIdClaim, out var actorId) ? actorId : null;
    }
}

public record RejectDocumentVerificationRequest
{
    public string Reason { get; init; } = string.Empty;
}
