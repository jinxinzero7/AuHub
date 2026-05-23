using FastEndpoints;
using Notifications.Application.Commands.SendNotification;
using Notifications.Domain.Enums;

namespace Notifications.API.Endpoints.Notifications;

public class SendNotificationEndpoint : Endpoint<SendNotificationRequest, Guid>
{
    private readonly SendNotificationCommandHandler _handler;

    public SendNotificationEndpoint(SendNotificationCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/notifications/send");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Send a notification (internal service-to-service)";
            s.Description = "Create a new notification for a user. Called internally by Auctions Service.";
        });
    }

    public override async Task HandleAsync(SendNotificationRequest req, CancellationToken ct)
    {
        // API Key validation for internal service-to-service calls
        var apiKey = HttpContext.Request.Headers["X-Internal-API-Key"].FirstOrDefault();
        var expectedApiKey = Environment.GetEnvironmentVariable("INTERNAL_API_KEY") ?? "AuHub-Internal-Secret-2026";
        
        if (string.IsNullOrEmpty(apiKey) || apiKey != expectedApiKey)
        {
            ThrowError("Unauthorized: Invalid or missing API Key", 401);
            return;
        }

        var command = new SendNotificationCommand
        {
            UserId = req.UserId,
            Type = (NotificationType)req.Type,
            Title = req.Title,
            Message = req.Message
        };

        var result = await _handler.HandleAsync(command, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        HttpContext.Response.StatusCode = 201;
        await HttpContext.Response.WriteAsJsonAsync(result.Value, ct);
    }
}

public class SendNotificationRequest
{
    public Guid UserId { get; set; }
    public int Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
