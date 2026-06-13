using System.Security.Claims;
using FastEndpoints;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;

namespace Identity.API.Endpoints.DocumentVerification;

public class ApproveDocumentVerificationRequestEndpoint : EndpointWithoutRequest<DocumentVerificationResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDocumentVerificationRequestRepository _requestRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;

    public ApproveDocumentVerificationRequestEndpoint(
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
        Post("/api/auth/document-verification/{id}/approve");
        Roles("Admin");
    }

    public override async Task HandleAsync(CancellationToken ct)
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
            request.Approve(adminId.Value);
            user.MarkDocumentVerified();

            await _auditLogRepository.AddAsync(AdminAuditLog.Create(adminId, "DocumentVerificationApprove", "DocumentVerificationRequest", request.Id, null), ct);
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
