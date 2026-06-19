using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Analysis.Dtos;
using Planora.Application.Features.Notifications.Dtos;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Notifications;
using Planora.Domain.Shared.Results;
using System.Text.Json;
using INotificationPublisher = Planora.Application.Interfaces.Services.INotificationPublisher;

namespace Planora.Application.Features.Analysis.Commands.AnalysisFailed;

public sealed class AnalysisFailedHandler(
    IAnalysisJobRepository analysisJobRepository,
    IParcelRepository parcelRepository,
    INotificationRepository notificationRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<AnalysisFailedHandler> logger) : IRequestHandler<AnalysisFailedCommand, Result<AnalysisJobProcessedResponse>>
{
    public async Task<Result<AnalysisJobProcessedResponse>> Handle(AnalysisFailedCommand request, CancellationToken ct)
    {
        logger.LogInformation("Processing analysis failed webhook for PythonJobId: {PythonJobId} with reason: {Reason}", request.PythonJobId, request.Reason);

        var analysisJob = await analysisJobRepository.GetByPythonJobIdAsync(request.PythonJobId, ct);

        if (analysisJob is null)
        {
            logger.LogWarning("AnalysisJob not found for PythonJobId: {PythonJobId}", request.PythonJobId);
            return AnalysisJobErrors.NotFound;
        }

        var markAsFailedResult = analysisJob.MarkAsFailed(request.Reason);

        if (markAsFailedResult.IsError)
        {
            logger.LogError("Failed to update status for AnalysisJob {AnalysisJobId}", analysisJob.Id);
            return AnalysisJobErrors.FaildStatusUpdate;
        }

        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await PublishFailureNotificationAsync(analysisJob, request.Reason, ct);

        logger.LogInformation("Successfully processed analysis failed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }

    private async Task PublishFailureNotificationAsync(AnalysisJob job, string reason, CancellationToken ct)
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

    private static string? ExtractLink(string? data) =>
        data is null ? null : JsonDocument.Parse(data).RootElement
            .TryGetProperty("link", out var l) ? l.GetString() : null;
}
