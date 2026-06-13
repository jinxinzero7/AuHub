using Identity.Domain.Entities;

namespace Identity.API.Endpoints.DocumentVerification;

public record DocumentVerificationResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string PassportImagePath { get; init; } = string.Empty;
    public string SelfieImagePath { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? ReviewedByAdminId { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime CreatedAt { get; init; }
}

public static class DocumentVerificationMappings
{
    public static DocumentVerificationResponse ToResponse(this DocumentVerificationRequest request)
    {
        return new DocumentVerificationResponse
        {
            Id = request.Id,
            UserId = request.UserId,
            PassportImagePath = request.PassportImagePath,
            SelfieImagePath = request.SelfieImagePath,
            Status = request.Status.ToString(),
            ReviewedByAdminId = request.ReviewedByAdminId,
            ReviewedAt = request.ReviewedAt,
            RejectionReason = request.RejectionReason,
            CreatedAt = request.CreatedAt
        };
    }
}
