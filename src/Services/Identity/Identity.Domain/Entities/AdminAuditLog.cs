namespace Identity.Domain.Entities;

public class AdminAuditLog
{
    public Guid Id { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string TargetType { get; private set; } = string.Empty;
    public Guid TargetId { get; private set; }
    public string? Details { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AdminAuditLog() { }

    public static AdminAuditLog Create(Guid? actorUserId, string action, string targetType, Guid targetId, string? details)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new InvalidOperationException("Audit action is required");

        if (string.IsNullOrWhiteSpace(targetType))
            throw new InvalidOperationException("Audit target type is required");

        var normalizedDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        if (normalizedDetails?.Length > 1000)
            throw new InvalidOperationException("Audit details are too long");

        return new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action.Trim(),
            TargetType = targetType.Trim(),
            TargetId = targetId,
            Details = normalizedDetails,
            CreatedAt = DateTime.UtcNow
        };
    }
}
