using AuHub.Shared.Results;
using Notifications.Application.Repositories;

namespace Notifications.Application.Queries.GetUserNotifications;

public class GetUserNotificationsQueryHandler
{
    private readonly INotificationRepository _repository;

    public GetUserNotificationsQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetUserNotificationsResponse>> HandleAsync(GetUserNotificationsQuery query, CancellationToken ct = default)
    {
        var notifications = await _repository.GetByUserIdAsync(query.UserId, query.Page, query.PageSize, query.OnlyUnread, ct);
        var totalCount = await _repository.CountByUserIdAsync(query.UserId, query.OnlyUnread, ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        var dtos = notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = (int)n.Type,
            Title = n.Title,
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();

        return Result.Success(new GetUserNotificationsResponse
        {
            Notifications = dtos,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }
}

public class GetUserNotificationsResponse
{
    public List<NotificationDto> Notifications { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}
