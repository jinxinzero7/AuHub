using FluentValidation;
using Identity.Domain.Entities;

namespace Identity.API.Endpoints.Auth;

public partial class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Must(phone => PhoneNumberRegex().IsMatch(User.NormalizePhoneNumber(phone)))
            .WithMessage("Invalid phone number format");

        RuleFor(x => x.Nickname)
            .NotEmpty().WithMessage("Nickname is required")
            .MinimumLength(3).WithMessage("Nickname must be at least 3 characters")
            .MaximumLength(32).WithMessage("Nickname must not exceed 32 characters")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Nickname can contain only Latin letters, numbers and underscore");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role");
    }

    [System.Text.RegularExpressions.GeneratedRegex("^\\+?[1-9]\\d{9,14}$")]
    private static partial System.Text.RegularExpressions.Regex PhoneNumberRegex();
}
