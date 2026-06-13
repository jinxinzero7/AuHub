namespace Identity.Domain.Entities;

public class DocumentVerificationRequest
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string PassportImagePath { get; private set; } = string.Empty;
    public string SelfieImagePath { get; private set; } = string.Empty;
    public DocumentVerificationRequestStatus Status { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public User User { get; private set; } = null!;

    private DocumentVerificationRequest() { }

    public static DocumentVerificationRequest Create(Guid userId, string passportImagePath, string selfieImagePath)
    {
        if (string.IsNullOrWhiteSpace(passportImagePath))
            throw new InvalidOperationException("Passport image path is required");

        if (string.IsNullOrWhiteSpace(selfieImagePath))
            throw new InvalidOperationException("Selfie image path is required");

        return new DocumentVerificationRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PassportImagePath = passportImagePath.Trim(),
            SelfieImagePath = selfieImagePath.Trim(),
            Status = DocumentVerificationRequestStatus.PendingReview,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve(Guid adminId)
    {
        EnsurePending();

        Status = DocumentVerificationRequestStatus.Approved;
        ReviewedByAdminId = adminId;
        ReviewedAt = DateTime.UtcNow;
        RejectionReason = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(Guid adminId, string reason)
    {
        EnsurePending();

        var normalizedReason = reason.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
            throw new InvalidOperationException("Rejection reason is required");

        if (normalizedReason.Length > 500)
            throw new InvalidOperationException("Rejection reason is too long");

        Status = DocumentVerificationRequestStatus.Rejected;
        ReviewedByAdminId = adminId;
        ReviewedAt = DateTime.UtcNow;
        RejectionReason = normalizedReason;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != DocumentVerificationRequestStatus.PendingReview)
            throw new InvalidOperationException("Only pending document verification requests can be reviewed");
    }
}
