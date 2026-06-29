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

        var topo = request.Payload;
        var elevation = topo.Elevation;
        var cutFill = topo.CutFillAnalysis;
        var ponding = topo.PondingRisk;
        var assets = topo.VisualizationAssets;
        var metadata = topo.Metadata;

        var slopeDistributionJson = topo.SlopeDistribution is not null
            ? JsonSerializer.Serialize(topo.SlopeDistribution)
            : string.Empty;

        var topographyResult = new TopographyResult(
            analysisJob.Id,
            elevation?.MinimumMeters ?? 0,
            elevation?.MaximumMeters ?? 0,
            elevation?.AverageMeters ?? 0,
            slopeDistributionJson,
            cutFill?.CutVolumeM3 ?? 0,
            cutFill?.FillVolumeM3 ?? 0,
            cutFill?.NetVolumeM3 ?? 0,
            contourInterval: 0,
            assets?.ContourGeoJsonUrl,
            assets?.PondingGeoJsonUrl,
            ponding?.ZonesCount,
            ponding?.AffectedAreaM2,
            assets?.ElevationTileUrl,
            assets?.SlopeTileUrl,
            assets?.DemRasterUrl,
            assets?.SlopeRasterUrl,
            metadata?.CopernicusDemVersion,
            metadata?.PixelResolutionMeters,
            metadata?.Crs,
            metadata?.ProcessingTimeSeconds);

        await topographyResultRepository.AddAsync(topographyResult, ct);
        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await AnalysisNotificationHelper.PublishAnalysisResultAsync(
            analysisJob, AiWebhookEventTypes.TopographyCompleted, request.Payload, notificationPublisher, ct);

        logger.LogInformation("Successfully processed topography completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
