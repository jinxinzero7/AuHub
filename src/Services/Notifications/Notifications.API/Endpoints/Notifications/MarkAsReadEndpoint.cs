using FastEndpoints;
using Notifications.Application.Commands.MarkAsRead;
using System.Security.Claims;

namespace Notifications.API.Endpoints.Notifications;

public class MarkAsReadEndpoint : EndpointWithoutRequest
{
    private readonly MarkAsReadCommandHandler _handler;

    public MarkAsReadEndpoint(MarkAsReadCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/notifications/{id}/read");
        Roles("Admin", "User");
        Summary(s =>
        {
            s.Summary = "Mark notification as read";
            s.Description = "Mark a notification as read. User must own the notification.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var notificationId = Route<Guid>("id");

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID in token", 401);
            return;
        }

        var command = new MarkAsReadCommand
        {
            NotificationId = notificationId,
            UserId = userId
        };

        var result = await _handler.HandleAsync(command, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        HttpContext.Response.StatusCode = 200;
    }
}
