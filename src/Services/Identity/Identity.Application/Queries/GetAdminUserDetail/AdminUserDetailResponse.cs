namespace Identity.Application.Queries.GetAdminUserDetail;

public record AdminUserDetailResponse
{
    public Guid UserId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Nickname { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsEmailVerified { get; init; }
    public DateTime? EmailVerifiedAt { get; init; }
    public bool IsPhoneVerified { get; init; }
    public DateTime? PhoneVerifiedAt { get; init; }
    public string DocumentVerificationStatus { get; init; } = string.Empty;
    public DateTime? DocumentVerifiedAt { get; init; }
    public bool IsBanned { get; init; }
    public DateTime? BannedAt { get; init; }
    public string? BanReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<AdminDocumentVerificationMetadata> DocumentVerificationHistory { get; init; } = [];
}

public record AdminDocumentVerificationMetadata
{
    public Guid RequestId { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? ReviewedByAdminId { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
