using Planora.Application.Features.Notifications.Dtos;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Notifications;
using System.Text.Json;
using INotificationPublisher = Planora.Application.Interfaces.Services.INotificationPublisher;

namespace Planora.Application.Common.Helpers;


internal static class AnalysisNotificationHelper
{
    public static async Task PublishCompletionNotificationAsync(
        AnalysisJob job,
        IParcelRepository parcelRepository,
        INotificationRepository notificationRepository,
        INotificationPublisher notificationPublisher,
        CancellationToken ct)
    {
        var parcel = await parcelRepository.GetByIdAsync(job.ParcelId, ct);
        if (parcel is null) return;

        var data = JsonSerializer.Serialize(new
        {
            parcelId = parcel.Id,
            moduleType = job.Type.ToString(),
            analysisJobId = job.Id,
            link = $"/parcels/{parcel.Id}/reports/{job.Type.ToString().ToLower()}"
        });

        var result = Notification.Create(
            id: Guid.NewGuid(),
            userId: parcel.UserId,
            type: NotificationType.ModuleCompleted,
            title: $"{job.Type} analysis complete",
            message: $"{job.Type} analysis complete for Parcel #{parcel.Id.ToString()[..8]}",
            data: data);

        if (result.IsError) return;

        await notificationRepository.AddAsync(result.Value, ct);
        var dto = new NotificationDto(
            result.Value.Id, result.Value.Type,
            result.Value.Title, result.Value.Message,
            Link: ExtractLink(data), Data: data,
            result.Value.CreatedAt, result.Value.IsRead);
        await notificationPublisher.PublishAsync(parcel.UserId, dto, ct);
    }

    public static async Task PublishFailureNotificationAsync(
        AnalysisJob job,
        string reason,
        IParcelRepository parcelRepository,
        INotificationRepository notificationRepository,
        INotificationPublisher notificationPublisher,
        CancellationToken ct)
    {
        var parcel = await parcelRepository.GetByIdAsync(job.ParcelId, ct);
        if (parcel is null) return;

        var data = JsonSerializer.Serialize(new
        {
            parcelId = parcel.Id,
            moduleType = job.Type.ToString(),
            analysisJobId = job.Id,
            reason,
            link = $"/parcels/{parcel.Id}/reports/{job.Type.ToString().ToLower()}"
        });

        var result = Notification.Create(
            id: Guid.NewGuid(),
            userId: parcel.UserId,
            type: NotificationType.ModuleFailed,
            title: $"{job.Type} analysis failed",
            message: $"{job.Type} analysis failed for Parcel #{parcel.Id.ToString()[..8]}: {reason}",
            data: data);

        if (result.IsError) return;

        await notificationRepository.AddAsync(result.Value, ct);
        var dto = new NotificationDto(
            result.Value.Id, result.Value.Type,
            result.Value.Title, result.Value.Message,
            Link: ExtractLink(data), Data: data,
            result.Value.CreatedAt, result.Value.IsRead);
        await notificationPublisher.PublishAsync(parcel.UserId, dto, ct);
    }

    public static string? ExtractLink(string? data) =>
        data is null ? null : JsonDocument.Parse(data).RootElement
            .TryGetProperty("link", out var l) ? l.GetString() : null;
}
