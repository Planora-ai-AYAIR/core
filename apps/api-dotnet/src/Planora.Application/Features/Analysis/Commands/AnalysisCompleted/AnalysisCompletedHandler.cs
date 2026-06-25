using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Analysis.Dtos;
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
    IParcelRepository parcelRepository,
    INotificationRepository notificationRepository,
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
            var slopeDistributionJson = topo.SlopeDistribution is not null
                ? JsonSerializer.Serialize(topo.SlopeDistribution) : string.Empty;

            var topographyResult = new TopographyResult(
                analysisJob.Id, topo.ElevationMin, topo.ElevationMax, topo.ElevationMean,
                slopeDistributionJson, topo.CutVolume, topo.FillVolume, topo.NetVolume,
                topo.ContourInterval, topo.ContourGeoJsonUrl, topo.PondingGeoJsonUrl,
                topo.PondingZonesCount, topo.PondingTotalArea, topo.ElevationTileUrl,
                topo.SlopeTileUrl, topo.DemRasterUrl, topo.SlopeRasterUrl,
                topo.Metadata?.CopernicusDemVersion, topo.Metadata?.PixelResolutionMeters,
                topo.Metadata?.Crs, topo.Metadata?.ProcessingTimeSeconds);

            await topographyResultRepository.AddAsync(topographyResult, ct);
        }

        if (result?.Soil is not null)
        {
            var soil = result.Soil;
            var bearing = result.Bearing;
            var spectralIndices = soil.SpectralIndices;

            var multiDepthProfileJson = soil.DepthProfiles is not null
                ? JsonSerializer.Serialize(soil.DepthProfiles) : null;
            var dataSourcesJson = soil.DataSources is not null
                ? JsonSerializer.Serialize(soil.DataSources) : null;
            var featureImportanceJson = bearing?.FeatureImportance is not null
                ? JsonSerializer.Serialize(bearing.FeatureImportance) : null;
            var soilFactorsJson = bearing?.SoilFactors is not null
                ? JsonSerializer.Serialize(bearing.SoilFactors) : null;
            var modelMetadata = bearing?.ModelMetadata;

            var soilResult = new SoilResult(
                analysisJob.Id, soil.SandPercent, soil.SiltPercent, soil.ClayPercent,
                soil.BulkDensity, soil.OrganicCarbon, soil.Ph,
                bearing?.BearingCapacityKpa ?? soil.BearingCapacityEstimate,
                bearing?.Classification ?? soil.BearingCapacityCategory,
                soil.CompositionUnit, soil.BulkDensityUnit, soil.OrganicCarbonUnit,
                soil.PrimaryType, soil.UsdaClass, soil.AiConfidence,
                multiDepthProfileJson, soil.HeatmapTileUrl, soil.Cec,
                soil.WaterTableDepthMeters, soil.SoilTypeGeoJsonUrl,
                soil.DepthProfileImageUrl, dataSourcesJson,
                spectralIndices?.NdviMean, spectralIndices?.BsiMean, spectralIndices?.NdmiMean,
                bearing?.Confidence, bearing?.Range, bearing?.TrafficLight,
                bearing?.RecommendedFoundation, bearing?.MaxFloorsWithoutDeepFoundation,
                bearing?.FloorCountCategory, bearing?.UncertaintyRange?.MinimumKpa,
                bearing?.UncertaintyRange?.MaximumKpa,
                featureImportanceJson, soilFactorsJson,
                modelMetadata?.ModelName, modelMetadata?.Framework,
                modelMetadata?.TrainingR2, modelMetadata?.ShapEnabled);

            await soilResultRepository.AddAsync(soilResult, ct);
        }
        else if (result?.Bearing is not null)
        {
            var bearing = result.Bearing;
            var featureImportanceJson = bearing.FeatureImportance is not null
                ? JsonSerializer.Serialize(bearing.FeatureImportance) : null;
            var soilFactorsJson = bearing.SoilFactors is not null
                ? JsonSerializer.Serialize(bearing.SoilFactors) : null;
            var modelMetadata = bearing.ModelMetadata;

            var soilResult = new SoilResult(
                analysisJob.Id, 0, 0, 0, 0, 0, 0,
                bearing.BearingCapacityKpa, bearing.Classification ?? "",
                bearingConfidence: bearing.Confidence,
                bearingRange: bearing.Range,
                bearingTrafficLight: bearing.TrafficLight,
                recommendedFoundation: bearing.RecommendedFoundation,
                maxFloorsWithoutDeepFoundation: bearing.MaxFloorsWithoutDeepFoundation,
                floorCountCategory: bearing.FloorCountCategory,
                bearingMinKpa: bearing.UncertaintyRange?.MinimumKpa,
                bearingMaxKpa: bearing.UncertaintyRange?.MaximumKpa,
                featureImportanceJson: featureImportanceJson,
                soilFactorsJson: soilFactorsJson,
                bearingModelName: modelMetadata?.ModelName,
                bearingFramework: modelMetadata?.Framework,
                bearingTrainingR2: modelMetadata?.TrainingR2,
                bearingShapEnabled: modelMetadata?.ShapEnabled);

            await soilResultRepository.AddAsync(soilResult, ct);
        }

        if (result?.Risk is not null)
        {
            var risk = result.Risk;
            var overallRiskLevel = risk.OverallRiskLevel ?? DeriveRiskLevel(risk.OverallRiskScore);
            var flood = risk.Flood;
            var seismic = risk.Seismic;
            var expansiveSoil = risk.ExpansiveSoil;
            var liquefaction = risk.Liquefaction;
            var mitigationSuggestionsJson = risk.MitigationSuggestions is not null
                ? JsonSerializer.Serialize(risk.MitigationSuggestions) : null;

            var riskResult = new RiskResult(
                analysisJob.Id,
                flood?.Score ?? risk.FloodRiskScore,
                seismic?.Score ?? risk.SeismicRiskScore,
                expansiveSoil?.Score ?? risk.ExpansiveSoilRisk,
                liquefaction?.Score ?? risk.LiquefactionRisk,
                risk.OverallRiskScore, overallRiskLevel,
                flood?.Level ?? DeriveRiskLevel(risk.FloodRiskScore),
                flood?.Factors is not null ? JsonSerializer.Serialize(flood.Factors) : null,
                flood?.GeoJsonUrl,
                seismic?.Level ?? DeriveRiskLevel(risk.SeismicRiskScore),
                seismic?.Factors is not null ? JsonSerializer.Serialize(seismic.Factors) : null,
                seismic?.Source, seismicZone: seismic?.Zone,
                expansiveSoil?.Level ?? DeriveRiskLevel(risk.ExpansiveSoilRisk),
                expansiveSoil?.Factors is not null ? JsonSerializer.Serialize(expansiveSoil.Factors) : null,
                expansiveSoil?.ReplacementDepth,
                liquefaction?.Level ?? DeriveRiskLevel(risk.LiquefactionRisk),
                liquefaction?.Factors is not null ? JsonSerializer.Serialize(liquefaction.Factors) : null,
                liquefaction?.Susceptibility, liquefactionMethodology: liquefaction?.Methodology,
                riskHeatmapTileUrl: risk.RiskHeatmapTileUrl,
                mitigationSuggestionsJson: mitigationSuggestionsJson);

            await riskResultRepository.AddAsync(riskResult, ct);
        }

        if (result?.Borehole is not null)
        {
            var borehole = result.Borehole;
            var placementPointsJson = borehole.PlacementPoints is not null
                ? JsonSerializer.Serialize(borehole.PlacementPoints) : null;

            var boreholeResult = new BoreholeResult(
                analysisJob.Id, borehole.MinimumRequired, borehole.OptimalCount,
                borehole.CoveragePercentage, borehole.GridSize, borehole.PlacementStrategy,
                placementPointsJson, borehole.PlacementGeoJsonUrl,
                borehole.TraditionalBoreholeCount, borehole.TraditionalEstimatedCost,
                borehole.OptimizedBoreholeCount, borehole.OptimizedEstimatedCost,
                borehole.SavingsAmount, borehole.SavingsPercentage, borehole.Currency);

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

        await AnalysisNotificationHelper.PublishCompletionNotificationAsync(
            analysisJob, parcelRepository, notificationRepository, notificationPublisher, ct);

        logger.LogInformation("Successfully processed aggregated analysis completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
