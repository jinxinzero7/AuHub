namespace Identity.Domain.Entities;

public class PhoneVerificationCode
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public User User { get; private set; } = null!;

    private PhoneVerificationCode() { }

    public static PhoneVerificationCode Create(Guid userId, string codeHash, DateTime expiresAt)
    {
        return new PhoneVerificationCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = codeHash,
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
