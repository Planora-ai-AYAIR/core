using Microsoft.AspNetCore.SignalR;
using Planora.Api.Hubs;
using Planora.Application.Features.Notifications.Dtos;
using Planora.Application.Interfaces.Services;

namespace Planora.Api.Services
{
    public sealed class SignalRNotificationPublisher(
    IHubContext<NotificationHub, INotificationClient> hub,
    ILogger<SignalRNotificationPublisher> logger)
    : INotificationPublisher
    {
        public async Task PublishAsync(Guid userId, NotificationDto dto, CancellationToken ct)
        {
            try
            {
                await hub.Clients.User(userId.ToString()).NotificationReceived(dto);
                logger.LogInformation(
                    "Pushed notification {NotificationId} to user {UserId}", dto.Id, userId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to push notification {NotificationId} to user {UserId}",
                    dto.Id, userId);
            }
        }
    }
}
