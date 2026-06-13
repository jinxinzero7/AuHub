using FastEndpoints;
using Identity.Application.Commands.Auth.EmailVerification;

namespace Identity.API.Endpoints.Auth;

public class ConfirmEmailVerificationEndpoint : Endpoint<ConfirmEmailVerificationRequest, ConfirmEmailVerificationResponse>
{
    private readonly ConfirmEmailVerificationCommandHandler _handler;

    public ConfirmEmailVerificationEndpoint(ConfirmEmailVerificationCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/auth/email-verification/confirm");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Confirm email verification";
            s.Description = "Marks a user's email as verified using a verification token";
        });
    }

    public override async Task HandleAsync(ConfirmEmailVerificationRequest req, CancellationToken ct)
    {
        var result = await _handler.HandleAsync(new ConfirmEmailVerificationCommand { Token = req.Token }, ct);
        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}

public record ConfirmEmailVerificationRequest
{
    public string Token { get; init; } = string.Empty;
}
