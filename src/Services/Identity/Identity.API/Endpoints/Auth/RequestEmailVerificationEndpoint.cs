using System.Security.Claims;
using FastEndpoints;
using Identity.Application.Commands.Auth.EmailVerification;

namespace Identity.API.Endpoints.Auth;

public class RequestEmailVerificationEndpoint : EndpointWithoutRequest<RequestEmailVerificationResponse>
{
    private readonly RequestEmailVerificationCommandHandler _handler;

    public RequestEmailVerificationEndpoint(RequestEmailVerificationCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/auth/email-verification/request");
        Roles("Admin", "User");
        Summary(s =>
        {
            s.Summary = "Request email verification";
            s.Description = "Creates an email verification token and sends it through the configured sender";
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

        var result = await _handler.HandleAsync(new RequestEmailVerificationCommand { UserId = userId }, ct);
        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}
