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

public sealed class BoreholeCompletedHandler(
    IAnalysisJobRepository analysisJobRepository,
    IBoreholeResultRepository boreholeResultRepository,
    IParcelRepository parcelRepository,
    INotificationRepository notificationRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<BoreholeCompletedHandler> logger) : IRequestHandler<BoreholeCompletedCommand, Result<AnalysisJobProcessedResponse>>
{
    public async Task<Result<AnalysisJobProcessedResponse>> Handle(BoreholeCompletedCommand request, CancellationToken ct)
    {
        logger.LogInformation("Processing borehole completed webhook for PythonJobId: {PythonJobId}", request.PythonJobId);

        var analysisJob = await analysisJobRepository.GetByPythonJobIdAsync(request.PythonJobId, ct);

        if (analysisJob is null)
        {
            logger.LogWarning("AnalysisJob not found for PythonJobId: {PythonJobId}", request.PythonJobId);
            return AnalysisJobErrors.NotFound;
        }

        if (analysisJob.Status != AnalysisJobStatus.Running)
        {
            logger.LogWarning("Invalid state transition for AnalysisJob {AnalysisJobId}. Current status: {Status}", analysisJob.Id, analysisJob.Status);
            return AnalysisJobErrors.InvalidStateTransition;
        }

        var markAsCompletedResult = analysisJob.MarkAsCompleted();
        if (markAsCompletedResult.IsError)
        {
            logger.LogError("Failed to update status for AnalysisJob {AnalysisJobId}", analysisJob.Id);
            return AnalysisJobErrors.FaildStatusUpdate;
        }

        var placementPointsJson = request.Payload.PlacementPoints is not null
            ? JsonSerializer.Serialize(request.Payload.PlacementPoints)
            : null;

        var boreholeResult = new BoreholeResult(
            analysisJob.Id,
            request.Payload.MinimumRequired,
            request.Payload.OptimalCount,
            request.Payload.CoveragePercentage,
            request.Payload.GridSize,
            request.Payload.PlacementStrategy,
            placementPointsJson,
            request.Payload.PlacementGeoJsonUrl,
            request.Payload.TraditionalBoreholeCount,
            request.Payload.TraditionalEstimatedCost,
            request.Payload.OptimizedBoreholeCount,
            request.Payload.OptimizedEstimatedCost,
            request.Payload.SavingsAmount,
            request.Payload.SavingsPercentage,
            request.Payload.Currency);

        await boreholeResultRepository.AddAsync(boreholeResult, ct);
        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await PublishCompletionNotificationAsync(analysisJob, ct);

        logger.LogInformation("Successfully processed borehole completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

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
