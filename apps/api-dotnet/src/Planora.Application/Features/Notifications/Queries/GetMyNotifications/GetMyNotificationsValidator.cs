using FluentValidation;

namespace Planora.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsValidator : AbstractValidator<GetMyNotificationsQuery>
{
    public GetMyNotificationsValidator()
    {
        RuleFor(x => x.Take).InclusiveBetween(1, 100);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
    }
}
