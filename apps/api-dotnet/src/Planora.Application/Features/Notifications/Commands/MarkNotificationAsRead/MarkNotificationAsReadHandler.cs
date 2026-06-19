using MediatR;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Notifications;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Notifications.Commands.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadHandler(
    IUser currentUser,
    INotificationRepository notifications)
    : IRequestHandler<MarkNotificationAsReadCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(
        MarkNotificationAsReadCommand cmd, CancellationToken ct)
    {
        if (currentUser.Id is not Guid userId)
            return Error.Unauthorized("Unauthorized", "User ID not found in token.");

        var notification = await notifications.GetByIdForUserAsync(cmd.Id, userId, ct);
        if (notification is null) return NotificationErrors.NotFound;

        if (notification.IsRead) return Result.Updated;

        notification.MarkAsRead();
        await notifications.UpdateAsync(notification, ct);

        return Result.Updated;
    }
}
