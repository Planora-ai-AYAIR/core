using System.Text.Json.Serialization;

namespace Planora.Application.Features.Parcels.Dtos.Analysis;

/// <summary>
/// Full analysis result response for US-BE-04.
/// Matches the GET /api/parcels/{parcelId}/analysis 200 contract.
/// </summary>
public sealed record ParcelAnalysisResponse(
    string PythonJobId,
    string BackendJobId,
    Guid ParcelId,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int ProcessingTimeSeconds,
    ParcelAnalysisResultDto Result,
    DateTime PresignedUrlsExpireAt);

public sealed record ParcelAnalysisResultDto(
    TopographyAnalysisDto? Topography,
    SoilAnalysisDto? Soil,
    BearingAnalysisDto? Bearing,
    RiskAnalysisDto? Risk,
    BoreholeAnalysisDto? Borehole);

// ── Topography ────────────────────────────────────────────────────────────

public sealed record TopographyAnalysisDto(
    ElevationAnalysisDto Elevation,
    List<SlopeDistributionDto> SlopeDistribution,
    CutFillAnalysisDto CutFillAnalysis,
    PondingRiskAnalysisDto PondingRisk,
    TopographyVisualizationAssetsDto VisualizationAssets,
    TopographyMetadataDto? Metadata);

public sealed record ElevationAnalysisDto(
    double MinimumMeters,
    double MaximumMeters,
    double AverageMeters,
    string Unit);

public sealed record SlopeDistributionDto(
    string Range,
    double Percentage);

public sealed record CutFillAnalysisDto(
    double CutVolumeM3,
    double FillVolumeM3,
    double NetVolumeM3,
    string Unit);

public sealed record PondingRiskAnalysisDto(
    string RiskLevel,
    int ZonesCount,
    double AffectedAreaM2,
    string Unit);

public sealed record TopographyVisualizationAssetsDto(
    string? ElevationTileUrl,
    string? SlopeTileUrl,
    string? ContourGeoJsonUrl,
    string? PondingGeoJsonUrl,
    string? DemRasterUrl,
    string? SlopeRasterUrl);

public sealed record TopographyMetadataDto(
    string? CopernicusDemVersion,
    int? PixelResolutionMeters,
    string? Crs,
    int? ProcessingTimeSeconds);

// ── Soil ─────────────────────────────────────────────────────────────────

public sealed record SoilAnalysisDto(
    SoilClassificationDto Classification,
    SoilSurfaceCompositionDto SurfaceComposition,
    SoilPropertiesDto Properties,
    List<SoilDepthLayerDto>? DepthLayers,
    SoilVisualizationAssetsDto VisualizationAssets,
    List<string>? DataSources,
    SoilSpectralIndicesDto? SpectralIndices);

public sealed record SoilClassificationDto(
    string PrimaryType,
    string UsdaClass,
    double? AiConfidence);

public sealed record SoilSurfaceCompositionDto(
    double SandPercentage,
    double SiltPercentage,
    double ClayPercentage,
    string Unit);

public sealed record SoilPropertiesDto(
    double BulkDensity,
    string BulkDensityUnit,
    double OrganicCarbonPercentage,
    double Ph,
    double Cec,
    double? WaterTableDepthMeters);

public sealed record SoilDepthLayerDto(
    string Depth,
    double Sand,
    double Silt,
    double Clay,
    string SoilType,
    double BulkDensity);

public sealed record SoilVisualizationAssetsDto(
    string? SoilHeatmapTileUrl,
    string? SoilTypeGeoJsonUrl,
    string? DepthProfileImageUrl);

public sealed record SoilSpectralIndicesDto(
    double NdviMean,
    double BsiMean,
    double NdmiMean);

// ── Bearing ───────────────────────────────────────────────────────────────

public sealed record BearingAnalysisDto(
    double BearingCapacityKpa,
    double Confidence,
    string Classification,
    string Range,
    string TrafficLight,
    string RecommendedFoundation,
    int? MaxFloorsWithoutDeepFoundation,
    string? FloorCountCategory,
    BearingUncertaintyRangeDto? UncertaintyRange,
    List<FeatureImportanceDto>? FeatureImportance,
    BearingSoilFactorsDto? SoilFactors,
    string Disclaimer,
    BearingModelMetadataDto? ModelMetadata);

public sealed record BearingUncertaintyRangeDto(
    double MinimumKpa,
    double MaximumKpa);

public sealed record FeatureImportanceDto(
    string Feature,
    double Weight);

public sealed record BearingSoilFactorsDto(
    double ClayContent,
    double SandContent,
    double MoistureIndex,
    double DepthToWaterTableMeters,
    double TerrainSlopePercent);

public sealed record BearingModelMetadataDto(
    string ModelName,
    string Framework,
    double TrainingR2,
    bool ShapEnabled);

// ── Risk ──────────────────────────────────────────────────────────────────

public sealed record RiskAnalysisDto(
    int OverallScore,
    string OverallRiskLevel,
    int MaxScore,
    RiskBreakdownDto RiskBreakdown,
    List<RiskMitigationSuggestionDto>? MitigationSuggestions,
    RiskVisualizationAssetsDto VisualizationAssets);

public sealed record RiskBreakdownDto(
    RiskSubCategoryDto Flood,
    RiskSubCategoryDto Seismic,
    RiskSubCategoryDto ExpansiveSoil,
    RiskSubCategoryDto Liquefaction);

public sealed record RiskSubCategoryDto(
    int Score,
    string Level,
    double Weight,
    List<string>? Factors,
    string? ZonesGeoJsonUrl,
    string? Zone,
    string? Source,
    double? ReplacementDepthMeters,
    string? Susceptibility,
    string? Methodology);

public sealed record RiskMitigationSuggestionDto(
    string RiskType,
    string Suggestion,
    string? CostImpact,
    string? Feasibility);

public sealed record RiskVisualizationAssetsDto(
    string? FloodRiskZonesGeoJsonUrl,
    string? RiskHeatmapTileUrl);

// ── Borehole ──────────────────────────────────────────────────────────────

public sealed record BoreholeAnalysisDto(
    BoreholeRecommendationDto Recommendation,
    List<BoreholeAnalysisPlacementPointDto>? PlacementPoints,
    BoreholeCostAnalysisDto CostAnalysis,
    BoreholeVisualizationAssetsDto VisualizationAssets);

public sealed record BoreholeAnalysisPlacementPointDto(
    string Id,
    double Latitude,
    double Longitude,
    string Priority,
    string? Reason = null,
    double? EstimatedDepthMeters = null);

public sealed record BoreholeRecommendationDto(
    int MinimumRequired,
    int OptimalCount,
    double CoveragePercentage,
    string? GridSize,
    string? Strategy);

public sealed record BoreholeCostAnalysisDto(
    BoreholeCostApproachDto TraditionalApproach,
    BoreholeCostApproachDto OptimizedApproach,
    BoreholeSavingsDto Savings);

public sealed record BoreholeCostApproachDto(
    int Boreholes,
    decimal EstimatedCost,
    string Currency,
    string? Basis);

public sealed record BoreholeSavingsDto(
    decimal Amount,
    string Currency,
    double Percentage);

public sealed record BoreholeVisualizationAssetsDto(
    string? BoreholePointsGeoJsonUrl);
