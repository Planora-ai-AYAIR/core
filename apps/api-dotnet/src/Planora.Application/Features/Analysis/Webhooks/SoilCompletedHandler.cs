using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Notifications.Dtos;
using Planora.Application.Features.Parcels.Dtos.Webhook;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Entities;
using Planora.Domain.Enums;
using Planora.Domain.Notifications;
using Planora.Domain.Shared.Results;
using INotificationPublisher = Planora.Application.Interfaces.Services.INotificationPublisher;

namespace Planora.Application.Features.Analysis.Webhooks;

public sealed class SoilCompletedHandler(
    IAnalysisJobRepository analysisJobRepository,
    ISoilResultRepository soilResultRepository,
    IParcelRepository parcelRepository,
    INotificationRepository notificationRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<SoilCompletedHandler> logger) : IRequestHandler<SoilCompletedCommand, Result<AnalysisJobProcessedResponse>>
{
    public async Task<Result<AnalysisJobProcessedResponse>> Handle(SoilCompletedCommand request, CancellationToken ct)
    {
        logger.LogInformation("Processing soil completed webhook for PythonJobId: {PythonJobId}", request.PythonJobId);

        var analysisJob = await analysisJobRepository.GetByPythonJobIdAsync(request.PythonJobId, ct);

        if (analysisJob is null)
        {
            logger.LogWarning("AnalysisJob not found for PythonJobId: {PythonJobId}", request.PythonJobId);
            return AnalysisJobErrors.NotFound;
        }

        var markAsCompletedResult = analysisJob.MarkAsCompleted();
        if (markAsCompletedResult.IsError)
        {
            logger.LogError("Failed to update status for AnalysisJob {AnalysisJobId}", analysisJob.Id);
            return AnalysisJobErrors.FaildStatusUpdate;
        }

        var multiDepthProfileJson = request.Payload.DepthProfiles is not null
            ? JsonSerializer.Serialize(request.Payload.DepthProfiles)
            : null;

        var soilResult = new SoilResult(
            analysisJob.Id,
            request.Payload.SandPercent,
            request.Payload.SiltPercent,
            request.Payload.ClayPercent,
            request.Payload.BulkDensity,
            request.Payload.OrganicCarbon,
            request.Payload.Ph,
            request.Payload.BearingCapacityEstimate,
            request.Payload.BearingCapacityCategory,
            request.Payload.CompositionUnit,
            request.Payload.BulkDensityUnit,
            request.Payload.OrganicCarbonUnit,
            request.Payload.PrimaryType,
            request.Payload.UsdaClass,
            request.Payload.AiConfidence,
            multiDepthProfileJson,
            request.Payload.HeatmapTileUrl);

        await soilResultRepository.AddAsync(soilResult, ct);
        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await PublishCompletionNotificationAsync(analysisJob, ct);

        logger.LogInformation("Successfully processed soil completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }

    private async Task PublishCompletionNotificationAsync(AnalysisJob job, CancellationToken ct)
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

    private static string? ExtractLink(string? data) =>
        data is null ? null : JsonDocument.Parse(data).RootElement
            .TryGetProperty("link", out var l) ? l.GetString() : null;
}
