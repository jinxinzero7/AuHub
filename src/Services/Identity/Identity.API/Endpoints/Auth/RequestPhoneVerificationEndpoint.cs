using System.Security.Claims;
using FastEndpoints;
using Identity.Application.Commands.Auth.PhoneVerification;

namespace Identity.API.Endpoints.Auth;

public class RequestPhoneVerificationEndpoint : EndpointWithoutRequest<RequestPhoneVerificationResponse>
{
    private readonly RequestPhoneVerificationCommandHandler _handler;

    public RequestPhoneVerificationEndpoint(RequestPhoneVerificationCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/auth/phone-verification/request");
        Roles("Admin", "User");
        Summary(s =>
        {
            s.Summary = "Request phone verification";
            s.Description = "Creates a phone verification code and sends it through the configured SMS sender";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user token", 401);
            return;
        }

        var result = await _handler.HandleAsync(new RequestPhoneVerificationCommand { UserId = userId }, ct);
        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}
