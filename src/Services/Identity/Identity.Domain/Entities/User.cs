namespace Identity.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Nickname { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public bool IsPhoneVerified { get; private set; }
    public DateTime? PhoneVerifiedAt { get; private set; }
    public UserDocumentVerificationStatus DocumentVerificationStatus { get; private set; }
    public DateTime? DocumentVerifiedAt { get; private set; }
    public bool IsBanned { get; private set; }
    public DateTime? BannedAt { get; private set; }
    public string? BanReason { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    public static User Create(
        string email,
        string phoneNumber,
        string nickname,
        string passwordHash,
        string name,
        UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PhoneNumber = NormalizePhoneNumber(phoneNumber),
            Nickname = nickname.Trim(),
            PasswordHash = passwordHash,
            Name = name,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false,
            IsPhoneVerified = false,
            DocumentVerificationStatus = UserDocumentVerificationStatus.Unverified,
            IsBanned = false
        };
    }

    public static string NormalizePhoneNumber(string phoneNumber)
    {
        return phoneNumber
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty);
    }

    public void MarkEmailVerified()
    {
        if (IsEmailVerified)
        {
            return;
        }

        IsEmailVerified = true;
        EmailVerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPhoneVerified()
    {
        if (IsPhoneVerified)
        {
            return;
        }

        IsPhoneVerified = true;
        PhoneVerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDocumentVerificationPending()
    {
        if (DocumentVerificationStatus == UserDocumentVerificationStatus.Verified)
        {
            return;
        }

        DocumentVerificationStatus = UserDocumentVerificationStatus.PendingReview;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDocumentVerified()
    {
        DocumentVerificationStatus = UserDocumentVerificationStatus.Verified;
        DocumentVerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDocumentUnverified()
    {
        if (DocumentVerificationStatus == UserDocumentVerificationStatus.Verified)
        {
            return;
        }

        DocumentVerificationStatus = UserDocumentVerificationStatus.Unverified;
        DocumentVerifiedAt = null;
        UpdatedAt = DateTime.UtcNow;
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
