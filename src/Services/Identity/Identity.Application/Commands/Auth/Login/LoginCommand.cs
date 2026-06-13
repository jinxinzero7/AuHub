namespace Identity.Application.Commands.Auth.Login;

public record LoginCommand
{
    public string Identifier { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    public string GetIdentifier()
    {
        return string.IsNullOrWhiteSpace(Identifier)
            ? Email
            : Identifier;
    }
}
