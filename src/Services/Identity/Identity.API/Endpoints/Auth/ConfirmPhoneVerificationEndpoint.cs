using System.Security.Claims;
using FastEndpoints;
using Identity.Application.Commands.Auth.PhoneVerification;

namespace Identity.API.Endpoints.Auth;

public class ConfirmPhoneVerificationEndpoint : Endpoint<ConfirmPhoneVerificationRequest, ConfirmPhoneVerificationResponse>
{
    private readonly ConfirmPhoneVerificationCommandHandler _handler;

    public ConfirmPhoneVerificationEndpoint(ConfirmPhoneVerificationCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/auth/phone-verification/confirm");
        Roles("Admin", "User");
        Summary(s =>
        {
            s.Summary = "Confirm phone verification";
            s.Description = "Marks a user's phone as verified using a 6-digit verification code";
        });
    }

    public override async Task HandleAsync(ConfirmPhoneVerificationRequest req, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user token", 401);
            return;
        }

        var result = await _handler.HandleAsync(
            new ConfirmPhoneVerificationCommand
            {
                UserId = userId,
                Code = req.Code
            },
            ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}

public record ConfirmPhoneVerificationRequest
{
    public string Code { get; init; } = string.Empty;
}
