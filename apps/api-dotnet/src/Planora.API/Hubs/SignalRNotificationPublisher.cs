using Microsoft.AspNetCore.SignalR;
using Planora.Application.Features.Analysis.Dtos.Realtime;
using Planora.Application.Features.Notifications.Dtos;
using Planora.Application.Interfaces.Services;

namespace Planora.Api.Hubs
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
        public async Task PublishToGroupAsync(string groupName, NotificationDto dto, CancellationToken ct)
        {
            try
            {
                await hub.Clients.Group(groupName).NotificationReceived(dto);
                logger.LogInformation(
                    "Pushed notification {NotificationId} to group {GroupName}", dto.Id, groupName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to push notification {NotificationId} to group {GroupName}",
                    dto.Id, groupName);
            }
        }

        public async Task PublishAnalysisResultAsync(Guid parcelId, AnalysisResultEnvelope envelope, CancellationToken ct)
        {
            try
            {
                var groupName = $"parcel:{parcelId}";
                await hub.Clients.Group(groupName).AnalysisResultReceived(envelope);
                logger.LogInformation(
                    "Pushed analysis result {EventType} for parcel {ParcelId} to group {GroupName}",
                    envelope.EventType, parcelId, groupName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to push analysis result {EventType} for parcel {ParcelId}",
                    envelope.EventType, parcelId);
            }
        }
    }
}
