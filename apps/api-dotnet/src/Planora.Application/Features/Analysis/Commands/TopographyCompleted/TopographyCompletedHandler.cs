using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Analysis.Dtos;
using Planora.Application.Features.Parcels.Dtos.Webhook;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Analysis;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;
using System.Text.Json;
using INotificationPublisher = Planora.Application.Interfaces.Services.INotificationPublisher;

namespace Planora.Application.Features.Analysis.Commands.TopographyCompleted;

public sealed class TopographyCompletedHandler(
    IAnalysisJobRepository analysisJobRepository,
    ITopographyResultRepository topographyResultRepository,
    IParcelRepository parcelRepository,
    INotificationRepository notificationRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<TopographyCompletedHandler> logger) : IRequestHandler<TopographyCompletedCommand, Result<AnalysisJobProcessedResponse>>
{
    public async Task<Result<AnalysisJobProcessedResponse>> Handle(TopographyCompletedCommand request, CancellationToken ct)
    {
        logger.LogInformation("Processing topography completed webhook for PythonJobId: {PythonJobId}", request.PythonJobId);

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

        var slopeDistributionJson = request.Payload.SlopeDistribution is not null
            ? JsonSerializer.Serialize(request.Payload.SlopeDistribution)
            : string.Empty;

        var topographyResult = new TopographyResult(
            analysisJob.Id,
            request.Payload.ElevationMin,
            request.Payload.ElevationMax,
            request.Payload.ElevationMean,
            slopeDistributionJson,
            request.Payload.CutVolume,
            request.Payload.FillVolume,
            request.Payload.NetVolume,
            request.Payload.ContourInterval,
            request.Payload.ContourGeoJsonUrl,
            request.Payload.PondingGeoJsonUrl,
            request.Payload.PondingZonesCount,
            request.Payload.PondingTotalArea,
            request.Payload.ElevationTileUrl,
            request.Payload.SlopeTileUrl,
            request.Payload.DemRasterUrl,
            request.Payload.SlopeRasterUrl,
            request.Payload.Metadata?.CopernicusDemVersion,
            request.Payload.Metadata?.PixelResolutionMeters,
            request.Payload.Metadata?.Crs,
            request.Payload.Metadata?.ProcessingTimeSeconds);

        await topographyResultRepository.AddAsync(topographyResult, ct);
        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await AnalysisNotificationHelper.PublishCompletionNotificationAsync(
            analysisJob, parcelRepository, notificationRepository, notificationPublisher, ct);

        await AnalysisNotificationHelper.PublishAnalysisResultAsync(
            analysisJob, AiWebhookEventTypes.TopographyCompleted, request.Payload, notificationPublisher, ct);

        logger.LogInformation("Successfully processed topography completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
