using FluentValidation;

namespace Auctions.Application.Commands.PlaceBid;

public class PlaceBidCommandValidator : AbstractValidator<PlaceBidCommand>
{
    public PlaceBidCommandValidator()
    {
        RuleFor(x => x.LotId)
            .NotEmpty()
            .WithMessage("Lot ID is required");

        RuleFor(x => x.BidderId)
            .NotEmpty()
            .WithMessage("Bidder ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Bid amount must be greater than 0");
    }
}
