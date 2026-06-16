using Planora.Application.Features.Notifications.Dtos;

namespace Planora.Api.Hubs
{
    public interface INotificationClient
    {
        Task NotificationReceived(NotificationDto notification);
    }
}
