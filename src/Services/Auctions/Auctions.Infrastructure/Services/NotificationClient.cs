using System.Net.Http.Json;
using System.Text.Json;
using Auctions.Application.Services;
using Auctions.Domain.Enums;
using AuHub.Shared.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Auctions.Infrastructure.Services;

public class NotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationClient> _logger;
    private readonly IConfiguration _configuration;

    public NotificationClient(HttpClient httpClient, ILogger<NotificationClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendNotificationAsync(Guid userId, NotificationType type, string title, string message, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                UserId = userId,
                Type = (int)type,
                Title = title,
                Message = message
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/notifications/send")
            {
                Content = JsonContent.Create(payload)
            };

            request.Headers.Add(InternalApiKey.HeaderName, InternalApiKey.GetExpectedValue(_configuration));

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Failed to send notification to user {UserId}. Status: {StatusCode}, Response: {Response}",
                    userId, response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
        }
    }
}
