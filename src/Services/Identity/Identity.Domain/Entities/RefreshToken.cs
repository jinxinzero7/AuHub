namespace Identity.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }

    // Rotation fields
    public Guid? FamilyId { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    // Приватный конструктор для EF Core
    private RefreshToken() { }

    // Фабричный метод создания
    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt, Guid? familyId = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false,
            FamilyId = familyId ?? Guid.NewGuid()
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    public void ReplaceBy(Guid newTokenId)
    {
        ReplacedByTokenId = newTokenId;
        Revoke();
    }

    public bool IsValid()
    {
        return !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }
}
