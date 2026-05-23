using FastEndpoints;
using Notifications.Application.Queries.GetUnreadCount;
using System.Security.Claims;

namespace Notifications.API.Endpoints.Notifications;

public class GetUnreadCountEndpoint : EndpointWithoutRequest<int>
{
    private readonly GetUnreadCountQueryHandler _handler;

    public GetUnreadCountEndpoint(GetUnreadCountQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/notifications/unread-count");
        Roles("Admin", "User");
        Summary(s =>
        {
            s.Summary = "Get unread notification count";
            s.Description = "Returns the number of unread notifications for the authenticated user.";
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

        var query = new GetUnreadCountQuery
        {
            UserId = userId
        };

        var result = await _handler.HandleAsync(query, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}
