using AuHub.Shared.Results;
using Identity.Domain.Interfaces;

namespace Identity.Application.Queries.GetAdminUserDetail;

public class GetAdminUserDetailQueryHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IDocumentVerificationRequestRepository _documentRepository;

    public GetAdminUserDetailQueryHandler(
        IUserRepository userRepository,
        IDocumentVerificationRequestRepository documentRepository)
    {
        _userRepository = userRepository;
        _documentRepository = documentRepository;
    }

    public async Task<Result<AdminUserDetailResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result.Failure<AdminUserDetailResponse>("User not found", 404);

        var documentRequests = await _documentRepository.GetByUserIdAsync(userId, cancellationToken);
        return Result.Success(user.ToAdminDetail(documentRequests));
    }
}
