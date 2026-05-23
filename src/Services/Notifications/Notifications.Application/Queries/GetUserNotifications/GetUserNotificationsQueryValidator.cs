using FluentValidation;

namespace Notifications.Application.Queries.GetUserNotifications;

public class GetUserNotificationsQueryValidator : AbstractValidator<GetUserNotificationsQuery>
{
    public GetUserNotificationsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100");
    }
}
