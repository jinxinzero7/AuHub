using FastEndpoints;
using Notifications.Application.Queries.GetUserNotifications;
using System.Security.Claims;

namespace Notifications.API.Endpoints.Notifications;

public class GetUserNotificationsEndpoint : EndpointWithoutRequest<GetUserNotificationsResponse>
{
    private readonly GetUserNotificationsQueryHandler _handler;

    public GetUserNotificationsEndpoint(GetUserNotificationsQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/notifications");
        Roles("Admin", "User");
        Summary(s =>
        {
            s.Summary = "Get current user notifications";
            s.Description = "Retrieve paginated notifications for the authenticated user with optional unread filter.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID in token", 401);
            return;
        }

        var page = Query<int>("page", isRequired: false);
        var pageSize = Query<int>("pageSize", isRequired: false);
        var onlyUnread = Query<bool>("onlyUnread", isRequired: false);

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = new GetUserNotificationsQuery
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize,
            OnlyUnread = onlyUnread
        };

        var result = await _handler.HandleAsync(query, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}
