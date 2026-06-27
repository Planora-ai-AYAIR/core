using Planora.Domain.Shared.Results;

namespace Planora.Domain.Notifications;

public static class NotificationErrors
{
    public static readonly Error TitleRequired =
        Error.Validation("Notification.Title.Required", "Notification title cannot be empty.");

    public static readonly Error MessageRequired =
        Error.Validation("Notification.Message.Required", "Notification message cannot be empty.");

    public static readonly Error NotFound =
        Error.NotFound("Notification.NotFound", "The requested notification was not found.");
}