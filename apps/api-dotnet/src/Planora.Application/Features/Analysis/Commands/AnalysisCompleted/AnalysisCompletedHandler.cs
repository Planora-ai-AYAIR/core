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

namespace Planora.Application.Features.Analysis.Commands.AnalysisCompleted;

public sealed class AnalysisCompletedHandler(
    IAnalysisJobRepository analysisJobRepository,
    ITopographyResultRepository topographyResultRepository,
    ISoilResultRepository soilResultRepository,
    IRiskResultRepository riskResultRepository,
    IBoreholeResultRepository boreholeResultRepository,
    IBearingResultRepository bearingResultRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<AnalysisCompletedHandler> logger) : IRequestHandler<AnalysisCompletedCommand, Result<AnalysisJobProcessedResponse>>
{
    private static string? DeriveRiskLevel(int score) => score switch
    {
        <= 20 => "Very Low",
        <= 40 => "Low",
        <= 60 => "Moderate",
        <= 80 => "High",
        _ => "Very High"
    };

    public async Task<Result<AnalysisJobProcessedResponse>> Handle(AnalysisCompletedCommand request, CancellationToken ct)
    {
        logger.LogInformation("Processing aggregated analysis completed webhook for PythonJobId: {PythonJobId}", request.PythonJobId);

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

        var result = request.Payload.Result;

        if (result?.Topography is not null)
        {
            var topo = result.Topography;
            var elevation = topo.Elevation;
            var cutFill = topo.CutFillAnalysis;
            var ponding = topo.PondingRisk;
            var assets = topo.VisualizationAssets;
            var metadata = topo.Metadata;

            var slopeDistributionJson = topo.SlopeDistribution is not null
                ? JsonSerializer.Serialize(topo.SlopeDistribution) : string.Empty;

            var topographyResult = new TopographyResult(
                analysisJob.Id,
                elevation?.MinimumMeters ?? 0, elevation?.MaximumMeters ?? 0, elevation?.AverageMeters ?? 0,
                slopeDistributionJson,
                cutFill?.CutVolumeM3 ?? 0, cutFill?.FillVolumeM3 ?? 0, cutFill?.NetVolumeM3 ?? 0,
                contourInterval: 0,
                assets?.ContourGeoJsonUrl, assets?.PondingGeoJsonUrl,
                ponding?.ZonesCount, ponding?.AffectedAreaM2,
                assets?.ElevationTileUrl, assets?.SlopeTileUrl,
                assets?.DemRasterUrl, assets?.SlopeRasterUrl,
                metadata?.CopernicusDemVersion, metadata?.PixelResolutionMeters,
                metadata?.Crs, metadata?.ProcessingTimeSeconds);

            await topographyResultRepository.AddAsync(topographyResult, ct);
        }

        if (result?.Soil is not null)
        {
            var soil = result.Soil;
            var classification = soil.Classification;
            var composition = soil.SurfaceComposition;
            var properties = soil.Properties;
            var assets = soil.VisualizationAssets;
            var spectralIndices = soil.SpectralIndices;

            var multiDepthProfileJson = SoilDepthLayerSerializer.Serialize(soil.DepthLayers);
            var dataSourcesJson = soil.DataSources is not null
                ? JsonSerializer.Serialize(soil.DataSources) : null;

            var soilResult = new SoilResult(
                analysisJob.Id,
                composition?.SandPercentage ?? 0, composition?.SiltPercentage ?? 0, composition?.ClayPercentage ?? 0,
                properties?.BulkDensity ?? 0, properties?.OrganicCarbonPercentage ?? 0, properties?.Ph ?? 0,
                composition?.Unit, properties?.BulkDensityUnit, organicCarbonUnit: null,
                classification?.PrimaryType, classification?.UsdaClass, classification?.AiConfidence,
                multiDepthProfileJson, assets?.SoilHeatmapTileUrl, properties?.Cec,
                properties?.WaterTableDepthMeters, assets?.SoilTypeGeoJsonUrl,
                assets?.DepthProfileImageUrl, dataSourcesJson,
                spectralIndices?.NdviMean, spectralIndices?.BsiMean, spectralIndices?.NdmiMean);

            await soilResultRepository.AddAsync(soilResult, ct);
        }

        if (result?.Bearing is not null)
        {
            var bearing = result.Bearing;
            var featureImportanceJson = bearing.FeatureImportance is not null
                ? JsonSerializer.Serialize(bearing.FeatureImportance) : null;
            var soilFactorsJson = bearing.SoilFactors is not null
                ? JsonSerializer.Serialize(bearing.SoilFactors) : null;
            var modelMetadata = bearing.ModelMetadata;

            var bearingResult = new BearingResult(
                analysisJob.Id, bearing.BearingCapacityKpa, bearing.Classification,
                bearing.Confidence, bearing.Range, bearing.TrafficLight,
                bearing.RecommendedFoundation, bearing.MaxFloorsWithoutDeepFoundation,
                bearing.FloorCountCategory,
                bearing.UncertaintyRange?.MinimumKpa, bearing.UncertaintyRange?.MaximumKpa,
                featureImportanceJson, soilFactorsJson,
                modelMetadata?.ModelName, modelMetadata?.Framework,
                modelMetadata?.TrainingR2, modelMetadata?.ShapEnabled);

            await bearingResultRepository.AddAsync(bearingResult, ct);
        }

        if (result?.Risk is not null)
        {
            var risk = result.Risk;
            var breakdown = risk.RiskBreakdown;
            var assets = risk.VisualizationAssets;
            var overallRiskLevel = risk.OverallRiskLevel ?? DeriveRiskLevel(risk.OverallScore);
            var flood = breakdown?.Flood;
            var seismic = breakdown?.Seismic;
            var expansiveSoil = breakdown?.ExpansiveSoil;
            var liquefaction = breakdown?.Liquefaction;
            var mitigationSuggestionsJson = risk.MitigationSuggestions is not null
                ? JsonSerializer.Serialize(risk.MitigationSuggestions) : null;

            var riskResult = new RiskResult(
                analysisJob.Id,
                flood?.Score ?? 0,
                seismic?.Score ?? 0,
                expansiveSoil?.Score ?? 0,
                liquefaction?.Score ?? 0,
                risk.OverallScore, overallRiskLevel,
                flood?.Level ?? DeriveRiskLevel(flood?.Score ?? 0),
                flood?.Factors is not null ? JsonSerializer.Serialize(flood.Factors) : null,
                flood?.ZonesGeoJsonUrl,
                seismic?.Level ?? DeriveRiskLevel(seismic?.Score ?? 0),
                seismic?.Factors is not null ? JsonSerializer.Serialize(seismic.Factors) : null,
                seismic?.Source, seismicZone: seismic?.Zone,
                expansiveSoil?.Level ?? DeriveRiskLevel(expansiveSoil?.Score ?? 0),
                expansiveSoil?.Factors is not null ? JsonSerializer.Serialize(expansiveSoil.Factors) : null,
                expansiveSoil?.ReplacementDepthMeters,
                liquefaction?.Level ?? DeriveRiskLevel(liquefaction?.Score ?? 0),
                liquefaction?.Factors is not null ? JsonSerializer.Serialize(liquefaction.Factors) : null,
                liquefaction?.Susceptibility, liquefactionMethodology: liquefaction?.Methodology,
                riskHeatmapTileUrl: assets?.RiskHeatmapTileUrl,
                mitigationSuggestionsJson: mitigationSuggestionsJson);

            await riskResultRepository.AddAsync(riskResult, ct);
        }

        if (result?.Borehole is not null)
        {
            var borehole = result.Borehole;
            var recommendation = borehole.Recommendation;
            var costAnalysis = borehole.CostAnalysis;
            var traditional = costAnalysis?.TraditionalApproach;
            var optimized = costAnalysis?.OptimizedApproach;
            var savings = costAnalysis?.Savings;
            var assets = borehole.VisualizationAssets;
            var placementPointsJson = borehole.PlacementPoints is not null
                ? JsonSerializer.Serialize(borehole.PlacementPoints) : null;

            var boreholeResult = new BoreholeResult(
                analysisJob.Id,
                recommendation?.MinimumRequired ?? 0, recommendation?.OptimalCount ?? 0,
                recommendation?.CoveragePercentage ?? 0, recommendation?.GridSize, recommendation?.Strategy,
                placementPointsJson, assets?.BoreholePointsGeoJsonUrl,
                traditional?.Boreholes ?? 0, traditional?.EstimatedCost ?? 0,
                optimized?.Boreholes ?? 0, optimized?.EstimatedCost ?? 0,
                savings?.Amount ?? 0, savings?.Percentage ?? 0,
                savings?.Currency ?? traditional?.Currency ?? optimized?.Currency);

            await boreholeResultRepository.AddAsync(boreholeResult, ct);
        }

        var markAsCompletedResult = analysisJob.MarkAsCompleted();
        if (markAsCompletedResult.IsError)
        {
            logger.LogError("Failed to update status for AnalysisJob {AnalysisJobId}", analysisJob.Id);
            return AnalysisJobErrors.FaildStatusUpdate;
        }

        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await AnalysisNotificationHelper.PublishAnalysisResultAsync(
            analysisJob, AiWebhookEventTypes.AnalysisCompleted, request.Payload, notificationPublisher, ct);

        logger.LogInformation("Successfully processed aggregated analysis completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
