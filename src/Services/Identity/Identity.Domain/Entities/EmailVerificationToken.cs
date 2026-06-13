namespace Identity.Domain.Entities;

public class EmailVerificationToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public User User { get; private set; } = null!;

    private EmailVerificationToken() { }

    public static EmailVerificationToken Create(Guid userId, string tokenHash, DateTime expiresAt)
    {
        return new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool CanBeUsed(DateTime now)
    {
        return UsedAt == null && ExpiresAt > now;
    }

    public void MarkUsed()
    {
        if (UsedAt != null)
        {
            return;
        }

        UsedAt = DateTime.UtcNow;
    }
}
