namespace Identity.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsBanned { get; private set; }
    public DateTime? BannedAt { get; private set; }
    public string? BanReason { get; private set; }

    // Navigation
    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // Приватный конструктор для EF Core
    private User() { }

    // Фабричный метод создания
    public static User Create(string email, string passwordHash, string name, UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Name = name,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            IsBanned = false
        };
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Ban(string reason)
    {
        IsBanned = true;
        BannedAt = DateTime.UtcNow;
        BanReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unban()
    {
        IsBanned = false;
        BannedAt = null;
        BanReason = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
