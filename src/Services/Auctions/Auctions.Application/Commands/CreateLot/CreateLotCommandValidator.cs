using FluentValidation;

namespace Auctions.Application.Commands.CreateLot;

public class CreateLotCommandValidator : AbstractValidator<CreateLotCommand>
{
    public CreateLotCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(3).WithMessage("Title must be at least 3 characters")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.StartingPrice)
            .Must(m => m.Amount > 0).WithMessage("Starting price must be greater than 0");

        RuleFor(x => x.DurationHours)
            .GreaterThan(0).WithMessage("Duration must be greater than 0")
            .LessThanOrEqualTo(720).WithMessage("Duration must not exceed 720 hours (30 days)");
    }
}
