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

namespace Planora.Application.Features.Analysis.Commands.SoilCompleted;

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

        var soil = request.Payload;
        var classification = soil.Classification;
        var composition = soil.SurfaceComposition;
        var properties = soil.Properties;
        var assets = soil.VisualizationAssets;
        var spectralIndices = soil.SpectralIndices;

        var multiDepthProfileJson = SoilDepthLayerSerializer.Serialize(soil.DepthLayers);

        var dataSourcesJson = soil.DataSources is not null
            ? JsonSerializer.Serialize(soil.DataSources)
            : null;

        var soilResult = new SoilResult(
            analysisJob.Id,
            composition?.SandPercentage ?? 0,
            composition?.SiltPercentage ?? 0,
            composition?.ClayPercentage ?? 0,
            properties?.BulkDensity ?? 0,
            properties?.OrganicCarbonPercentage ?? 0,
            properties?.Ph ?? 0,
            composition?.Unit,
            properties?.BulkDensityUnit,
            organicCarbonUnit: null,
            classification?.PrimaryType,
            classification?.UsdaClass,
            classification?.AiConfidence,
            multiDepthProfileJson,
            assets?.SoilHeatmapTileUrl,
            cec: properties?.Cec,
            waterTableDepthMeters: properties?.WaterTableDepthMeters,
            soilTypeGeoJsonUrl: assets?.SoilTypeGeoJsonUrl,
            depthProfileImageUrl: assets?.DepthProfileImageUrl,
            dataSourcesJson: dataSourcesJson,
            ndviMean: spectralIndices?.NdviMean,
            bsiMean: spectralIndices?.BsiMean,
            ndmiMean: spectralIndices?.NdmiMean);

        await soilResultRepository.AddAsync(soilResult, ct);
        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);
        
        await AnalysisNotificationHelper.PublishAnalysisResultAsync(
            analysisJob, AiWebhookEventTypes.SoilCompleted, request.Payload, notificationPublisher, ct);

        logger.LogInformation("Successfully processed soil completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
