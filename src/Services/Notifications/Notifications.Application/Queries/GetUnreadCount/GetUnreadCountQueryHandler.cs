using AuHub.Shared.Results;
using Notifications.Application.Repositories;

namespace Notifications.Application.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler
{
    private readonly INotificationRepository _repository;

    public GetUnreadCountQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> HandleAsync(GetUnreadCountQuery query, CancellationToken ct = default)
    {
        var count = await _repository.CountByUserIdAsync(query.UserId, onlyUnread: true, ct);
        return Result.Success(count);
    }
}
