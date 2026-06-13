using Auctions.Domain.Enums;

namespace Auctions.Domain.Entities;

public class TrustScoreEvent
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public TrustScoreSubject Subject { get; private set; }
    public TrustScoreReason Reason { get; private set; }
    public int Points { get; private set; }
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid ReferenceId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TrustScoreEvent() { }

    public static TrustScoreEvent Create(
        Guid userId,
        TrustScoreSubject subject,
        TrustScoreReason reason,
        int points,
        string referenceType,
        Guid referenceId)
    {
        if (points == 0)
            throw new InvalidOperationException("Trust score event points cannot be zero");

        if (string.IsNullOrWhiteSpace(referenceType))
            throw new InvalidOperationException("Trust score event reference type is required");

        return new TrustScoreEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Subject = subject,
            Reason = reason,
            Points = points,
            ReferenceType = referenceType.Trim(),
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
