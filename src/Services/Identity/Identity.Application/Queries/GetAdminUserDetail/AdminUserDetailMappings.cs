using Identity.Domain.Entities;

namespace Identity.Application.Queries.GetAdminUserDetail;

public static class AdminUserDetailMappings
{
    public static AdminUserDetailResponse ToAdminDetail(
        this User user,
        IEnumerable<DocumentVerificationRequest> documentRequests)
    {
        return new AdminUserDetailResponse
        {
            UserId = user.Id,
            Role = user.Role.ToString(),
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Nickname = user.Nickname,
            Name = user.Name,
            IsEmailVerified = user.IsEmailVerified,
            EmailVerifiedAt = user.EmailVerifiedAt,
            IsPhoneVerified = user.IsPhoneVerified,
            PhoneVerifiedAt = user.PhoneVerifiedAt,
            DocumentVerificationStatus = user.DocumentVerificationStatus.ToString(),
            DocumentVerifiedAt = user.DocumentVerifiedAt,
            IsBanned = user.IsBanned,
            BannedAt = user.BannedAt,
            BanReason = user.BanReason,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            DocumentVerificationHistory = documentRequests
                .OrderByDescending(request => request.CreatedAt)
                .Select(request => new AdminDocumentVerificationMetadata
                {
                    RequestId = request.Id,
                    Status = request.Status.ToString(),
                    ReviewedByAdminId = request.ReviewedByAdminId,
                    ReviewedAt = request.ReviewedAt,
                    RejectionReason = request.RejectionReason,
                    CreatedAt = request.CreatedAt,
                    UpdatedAt = request.UpdatedAt
                })
                .ToList()
        };
    }
}
