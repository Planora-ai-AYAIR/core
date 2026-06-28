using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Options;
using Planora.Application.Features.Parcels.Dtos.Analysis;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Analysis;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetParcelAnalysis;

public sealed class GetParcelAnalysisQueryHandler(
    IParcelRepository parcelRepository,
    IAnalysisJobRepository analysisJobRepository,
    ITopographyResultRepository topographyResultRepository,
    ISoilResultRepository soilResultRepository,
    IRiskResultRepository riskResultRepository,
    IBoreholeResultRepository boreholeResultRepository,
    IBearingResultRepository bearingResultRepository,
    IStorageService storageService,
    IHybridCacheService cacheService,
    ILogger<GetParcelAnalysisQueryHandler> logger)
    : IRequestHandler<GetParcelAnalysisQuery, Result<ParcelAnalysisResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly TimeSpan UrlExpiry = TimeSpan.FromHours(1);
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(50);

    public async Task<Result<ParcelAnalysisResponse>> Handle(
        GetParcelAnalysisQuery request,
        CancellationToken ct)
    {
        // 0. Authorization: verify the parcel belongs to the requesting user.
        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);
        if (parcel is null || parcel.UserId != request.UserId)
        {
            logger.LogWarning(
                "Analysis requested for inaccessible parcel. ParcelId: {ParcelId}, UserId: {UserId}",
                request.ParcelId, request.UserId);
            return ParcelErrors.NotFound;
        }

        var cacheKey = $"analysis:{request.ParcelId}";

        // 1. Fast path: Redis cache hit
        var cached = await cacheService.GetAsync<ParcelAnalysisResponse>(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for parcel analysis. ParcelId: {ParcelId}", request.ParcelId);
            return cached;
        }

        logger.LogInformation("Cache miss — assembling full analysis for ParcelId: {ParcelId}", request.ParcelId);

        // 2. Load all analysis jobs for this parcel
        var jobs = await analysisJobRepository.GetByParcelIdAsync(request.ParcelId, ct);

        // 3. Determine aggregate status — if not all completed, return 409 Conflict
        var aggregateStatus = ComputeAggregateStatus(jobs);
        if (aggregateStatus != "Completed")
        {
            var progress = ComputeProgress(jobs);
            logger.LogInformation(
                "Analysis not completed for ParcelId: {ParcelId}. Status: {Status}, Progress: {Progress}%",
                request.ParcelId, aggregateStatus, progress);

            return AnalysisJobErrors.AnalysisNotCompletedWithProgress(aggregateStatus, progress);
        }

        // 4. Find the job that owns the module result rows.
        //    Aggregated flow: AnalysisCompletedHandler saves ALL module results
        //    keyed to the single Aggregated job's Id. Per-module flow falls back
        //    to the type-specific jobs.
        var aggregatedJob = FindCompletedJob(jobs, AnalysisType.Aggregated);
        var topoJob  = aggregatedJob ?? FindCompletedJob(jobs, AnalysisType.Topography);
        var soilJob  = aggregatedJob ?? FindCompletedJob(jobs, AnalysisType.Soil);
        var riskJob  = aggregatedJob ?? FindCompletedJob(jobs, AnalysisType.Risk);
        var boreJob  = aggregatedJob ?? FindCompletedJob(jobs, AnalysisType.Borehole);
        var bearingJob = aggregatedJob ?? FindCompletedJob(jobs, AnalysisType.Soil);

        // 5. Load module results sequentially (DbContext is not thread-safe).
        var topographyResult = await LoadResultAsync(topoJob, topographyResultRepository.GetByAnalysisJobIdAsync, ct);
        var soilResult       = await LoadResultAsync(soilJob, soilResultRepository.GetByAnalysisJobIdAsync, ct);
        var riskResult       = await LoadResultAsync(riskJob, riskResultRepository.GetByAnalysisJobIdAsync, ct);
        var boreholeResult   = await LoadResultAsync(boreJob, boreholeResultRepository.GetByAnalysisJobIdAsync, ct);
        var bearingResult    = await LoadResultAsync(bearingJob, bearingResultRepository.GetByAnalysisJobIdAsync, ct);

        // 6. Generate presigned URLs concurrently for each module
        var topoAssetsTask  = PresignTopographyAssetsAsync(topographyResult, ct);
        var soilAssetsTask  = PresignSoilAssetsAsync(soilResult, ct);
        var riskAssetsTask  = PresignRiskAssetsAsync(riskResult, ct);
        var boreAssetsTask  = PresignBoreholeAssetsAsync(boreholeResult, ct);

        await Task.WhenAll(topoAssetsTask, soilAssetsTask, riskAssetsTask, boreAssetsTask);

        var topoAssets = await topoAssetsTask;
        var soilAssets = await soilAssetsTask;
        var riskAssets = await riskAssetsTask;
        var boreAssets = await boreAssetsTask;

        var expireAt = DateTime.UtcNow.Add(UrlExpiry);

        // 7. Build the aggregated job metadata
        var primaryJob = jobs.FirstOrDefault(j => j.Type == AnalysisType.Aggregated)
                         ?? jobs.First();

        var startedAt = jobs.Min(j => j.CreatedAt);
        var completedAt = jobs
            .Where(j => j.CompletedAt.HasValue)
            .Max(j => j.CompletedAt);
        var processingSeconds = completedAt.HasValue
            ? (int)(completedAt.Value - startedAt).TotalSeconds
            : 0;

        // 8. Assemble each module DTO
        var topographyDto = topographyResult is not null
            ? BuildTopographyDto(topographyResult, topoAssets)
            : null;

        var soilDto = soilResult is not null
            ? BuildSoilDto(soilResult, soilAssets)
            : null;

        var bearingDto = bearingResult is not null
            ? BuildBearingDto(bearingResult)
            : null;

        var riskDto = riskResult is not null
            ? BuildRiskDto(riskResult, riskAssets)
            : null;

        var boreholeDto = boreholeResult is not null
            ? BuildBoreholeDto(boreholeResult, boreAssets)
            : null;

        var response = new ParcelAnalysisResponse(
            PythonJobId: primaryJob.PythonJobId,
            BackendJobId: primaryJob.Id.ToString(),
            ParcelId: request.ParcelId,
            Status: "Completed",
            StartedAt: startedAt,
            CompletedAt: completedAt,
            ProcessingTimeSeconds: processingSeconds,
            Result: new ParcelAnalysisResultDto(
                topographyDto, soilDto, bearingDto, riskDto, boreholeDto),
            PresignedUrlsExpireAt: expireAt);

        // 9. Cache for 50 minutes (under 1-hour URL expiry so cached URLs remain valid)
        await cacheService.SetAsync(cacheKey, response, new CacheEntryOptions
        {
            Expiration = CacheExpiry,
            LocalCacheExpiration = TimeSpan.FromMinutes(5),
            Tags = ["analysis", $"parcel:{request.ParcelId}"]
        }, ct);

        logger.LogInformation(
            "Full analysis assembled and cached for ParcelId: {ParcelId}. PresignedUrlsExpireAt: {ExpiresAt}",
            request.ParcelId, expireAt);

        return response;
    }

    // ─── Status & Progress Helpers ──────────────────────────────────────────

    private static string ComputeAggregateStatus(IReadOnlyList<AnalysisJob> jobs)
    {
        if (jobs.Count == 0) return "Pending";
        if (jobs.Any(j => j.Status == AnalysisJobStatus.Failed)) return "Failed";
        if (jobs.All(j => j.Status == AnalysisJobStatus.Completed)) return "Completed";
        return "Processing";
    }

    private static int ComputeProgress(IReadOnlyList<AnalysisJob> jobs)
    {
        if (jobs.Count == 0) return 0;
        var completed = jobs.Count(j => j.Status == AnalysisJobStatus.Completed);
        return (int)Math.Round((double)completed / jobs.Count * 100);
    }

    private static AnalysisJob? FindCompletedJob(IReadOnlyList<AnalysisJob> jobs, AnalysisType type) =>
        jobs.FirstOrDefault(j => j.Type == type && j.Status == AnalysisJobStatus.Completed);

    // ─── Result Loading ─────────────────────────────────────────────────────

    private static async Task<TResult?> LoadResultAsync<TResult>(
        AnalysisJob? job,
        Func<Guid, CancellationToken, Task<TResult?>> fetch,
        CancellationToken ct)
        where TResult : class
    {
        if (job is null) return null;
        return await fetch(job.Id, ct);
    }

    // ─── Presigned URL Generation (returns immutable DTOs) ──────────────────

    private async Task<TopographyVisualizationAssetsDto?> PresignTopographyAssetsAsync(
        TopographyResult? result, CancellationToken ct)
    {
        if (result is null) return null;

        var contour   = await storageService.TryGetPreSignedUrlAsync(result.ContourGeoJsonUrl, UrlExpiry, ct);
        var ponding   = await storageService.TryGetPreSignedUrlAsync(result.PondingGeoJsonUrl, UrlExpiry, ct);
        var elevation = await storageService.TryGetPreSignedUrlAsync(result.ElevationTileUrl, UrlExpiry, ct);
        var slope     = await storageService.TryGetPreSignedUrlAsync(result.SlopeTileUrl, UrlExpiry, ct);
        var dem       = await storageService.TryGetPreSignedUrlAsync(result.DemRasterUrl, UrlExpiry, ct);
        var slopeRas  = await storageService.TryGetPreSignedUrlAsync(result.SlopeRasterUrl, UrlExpiry, ct);

        return new TopographyVisualizationAssetsDto(
            ElevationTileUrl: elevation,
            SlopeTileUrl: slope,
            ContourGeoJsonUrl: contour,
            PondingGeoJsonUrl: ponding,
            DemRasterUrl: dem,
            SlopeRasterUrl: slopeRas);
    }

    private async Task<SoilVisualizationAssetsDto?> PresignSoilAssetsAsync(
        SoilResult? result, CancellationToken ct)
    {
        if (result is null) return null;

        var heatmap      = await storageService.TryGetPreSignedUrlAsync(result.HeatmapTileUrl, UrlExpiry, ct);
        var soilType     = await storageService.TryGetPreSignedUrlAsync(result.SoilTypeGeoJsonUrl, UrlExpiry, ct);
        var depthProfile = await storageService.TryGetPreSignedUrlAsync(result.DepthProfileImageUrl, UrlExpiry, ct);

        return new SoilVisualizationAssetsDto(
            SoilHeatmapTileUrl: heatmap,
            SoilTypeGeoJsonUrl: soilType,
            DepthProfileImageUrl: depthProfile);
    }

    private async Task<RiskVisualizationAssetsDto?> PresignRiskAssetsAsync(
        RiskResult? result, CancellationToken ct)
    {
        if (result is null) return null;

        var flood     = await storageService.TryGetPreSignedUrlAsync(result.FloodGeoJsonUrl, UrlExpiry, ct);
        var heatTile  = await storageService.TryGetPreSignedUrlAsync(result.RiskHeatmapTileUrl, UrlExpiry, ct);

        return new RiskVisualizationAssetsDto(
            FloodRiskZonesGeoJsonUrl: flood,
            RiskHeatmapTileUrl: heatTile);
    }

    private async Task<BoreholeVisualizationAssetsDto?> PresignBoreholeAssetsAsync(
        BoreholeResult? result, CancellationToken ct)
    {
        if (result is null) return null;

        var placement = await storageService.TryGetPreSignedUrlAsync(result.PlacementGeoJsonUrl, UrlExpiry, ct);

        return new BoreholeVisualizationAssetsDto(
            BoreholePointsGeoJsonUrl: placement);
    }

    // ─── DTO Assembly ────────────────────────────────────────────────────────

    private static TopographyAnalysisDto BuildTopographyDto(
        TopographyResult r, TopographyVisualizationAssetsDto? assets)
    {
        var slopeDistribution = DeserializeJson<List<SlopeDistributionEntry>>(r.SlopeDistributionJson)
            ?.Select(s => new SlopeDistributionDto(s.Range, s.Percentage))
            .ToList() ?? [];

        var metadata = (r.CopernicusDemVersion is not null ||
                        r.PixelResolutionMeters is not null ||
                        r.Crs is not null ||
                        r.ProcessingTimeSeconds is not null)
            ? new TopographyMetadataDto(
                CopernicusDemVersion: r.CopernicusDemVersion,
                PixelResolutionMeters: r.PixelResolutionMeters,
                Crs: r.Crs,
                ProcessingTimeSeconds: r.ProcessingTimeSeconds)
            : null;

        return new TopographyAnalysisDto(
            Elevation: new ElevationAnalysisDto(r.ElevationMin, r.ElevationMax, r.ElevationMean, "m"),
            SlopeDistribution: slopeDistribution,
            CutFillAnalysis: new CutFillAnalysisDto(r.CutVolume, r.FillVolume, r.NetVolume, "m3"),
            PondingRisk: new PondingRiskAnalysisDto(
                DerivePondingRiskLevel(r.PondingZonesCount, r.PondingTotalArea),
                r.PondingZonesCount ?? 0,
                r.PondingTotalArea ?? 0,
                "m2"),
            VisualizationAssets: assets ?? new TopographyVisualizationAssetsDto(null, null, null, null, null, null),
            Metadata: metadata);
    }

    private static SoilAnalysisDto BuildSoilDto(SoilResult r, SoilVisualizationAssetsDto? assets)
    {
        var depthEntries = DeserializeJson<List<DepthProfileEntry>>(r.MultiDepthProfileJson);
        var depthLayers = depthEntries?
            .Select(d => new SoilDepthLayerDto(
                d.Depth, d.Sand, d.Silt, d.Clay, d.Type, d.BulkDensity ?? 0))
            .ToList();

        var dataSources = DeserializeJson<List<string>>(r.DataSourcesJson);

        var spectralIndices = (r.NdviMean.HasValue || r.BsiMean.HasValue || r.NdmiMean.HasValue)
            ? new SoilSpectralIndicesDto(
                NdviMean: r.NdviMean ?? 0,
                BsiMean: r.BsiMean ?? 0,
                NdmiMean: r.NdmiMean ?? 0)
            : null;

        return new SoilAnalysisDto(
            Classification: new SoilClassificationDto(
                r.PrimaryType ?? "",
                r.UsdaClass ?? "",
                r.AiConfidence),
            SurfaceComposition: new SoilSurfaceCompositionDto(
                r.SandPercent, r.SiltPercent, r.ClayPercent,
                r.CompositionUnit ?? "%"),
            Properties: new SoilPropertiesDto(
                r.BulkDensity, r.BulkDensityUnit ?? "g/cm3",
                r.OrganicCarbon, r.Ph,
                r.Cec ?? 0, r.WaterTableDepthMeters),
            DepthLayers: depthLayers,
            VisualizationAssets: assets ?? new SoilVisualizationAssetsDto(null, null, null),
            DataSources: dataSources,
            SpectralIndices: spectralIndices);
    }

    private static BearingAnalysisDto? BuildBearingDto(BearingResult r)
    {
        if (r.BearingCapacityKpa <= 0 && string.IsNullOrEmpty(r.Classification))
            return null;

        var uncertaintyRange = (r.MinKpa.HasValue && r.MaxKpa.HasValue)
            ? new BearingUncertaintyRangeDto(r.MinKpa.Value, r.MaxKpa.Value)
            : null;

        var featureImportance = DeserializeJson<List<FeatureImportanceEntry>>(r.FeatureImportanceJson)
            ?.Select(f => new FeatureImportanceDto(f.Feature, f.Weight))
            .ToList();

        var soilFactors = DeserializeJson<SoilFactorsEntry>(r.SoilFactorsJson) is { } sf
            ? new BearingSoilFactorsDto(
                sf.ClayContent, sf.SandContent, sf.MoistureIndex,
                sf.DepthToWaterTableMeters, sf.TerrainSlopePercent)
            : null;

        var modelMetadata = r.ModelName is not null
            ? new BearingModelMetadataDto(
                ModelName: r.ModelName,
                Framework: r.Framework ?? "",
                TrainingR2: r.TrainingR2 ?? 0,
                ShapEnabled: r.ShapEnabled ?? false)
            : null;

        return new BearingAnalysisDto(
            BearingCapacityKpa: r.BearingCapacityKpa,
            Confidence: r.Confidence ?? 0,
            Classification: r.Classification ?? "",
            Range: r.Range ?? "",
            TrafficLight: r.TrafficLight ?? "",
            RecommendedFoundation: r.RecommendedFoundation ?? "",
            MaxFloorsWithoutDeepFoundation: r.MaxFloorsWithoutDeepFoundation,
            FloorCountCategory: r.FloorCountCategory,
            UncertaintyRange: uncertaintyRange,
            FeatureImportance: featureImportance,
            SoilFactors: soilFactors,
            Disclaimer: "Pre-qualification estimate. Physical borehole verification required before structural design.",
            ModelMetadata: modelMetadata);
    }

    private static RiskAnalysisDto BuildRiskDto(RiskResult r, RiskVisualizationAssetsDto? assets)
    {
        var flood = new RiskSubCategoryDto(
            Score: r.FloodRiskScore,
            Level: r.FloodLevel ?? DeriveRiskLevel(r.FloodRiskScore),
            Weight: 0.25,
            Factors: DeserializeJson<List<string>>(r.FloodFactorsJson),
            ZonesGeoJsonUrl: assets?.FloodRiskZonesGeoJsonUrl,
            Zone: null,
            Source: null,
            ReplacementDepthMeters: null,
            Susceptibility: null,
            Methodology: null);

        var seismic = new RiskSubCategoryDto(
            Score: r.SeismicRiskScore,
            Level: r.SeismicLevel ?? DeriveRiskLevel(r.SeismicRiskScore),
            Weight: 0.20,
            Factors: DeserializeJson<List<string>>(r.SeismicFactorsJson),
            ZonesGeoJsonUrl: null,
            Zone: r.SeismicZone,
            Source: r.SeismicSource,
            ReplacementDepthMeters: null,
            Susceptibility: null,
            Methodology: null);

        var expansiveSoil = new RiskSubCategoryDto(
            Score: r.ExpansiveSoilRisk,
            Level: r.ExpansiveSoilLevel ?? DeriveRiskLevel(r.ExpansiveSoilRisk),
            Weight: 0.30,
            Factors: DeserializeJson<List<string>>(r.ExpansiveSoilFactorsJson),
            ZonesGeoJsonUrl: null, Zone: null, Source: null,
            ReplacementDepthMeters: r.ReplacementDepth,
            Susceptibility: null, Methodology: null);

        var liquefaction = new RiskSubCategoryDto(
            Score: r.LiquefactionRisk,
            Level: r.LiquefactionLevel ?? DeriveRiskLevel(r.LiquefactionRisk),
            Weight: 0.25,
            Factors: DeserializeJson<List<string>>(r.LiquefactionFactorsJson),
            ZonesGeoJsonUrl: null, Zone: null, Source: null,
            ReplacementDepthMeters: null,
            Susceptibility: r.LiquefactionSusceptibility,
            Methodology: r.LiquefactionMethodology);

        var mitigationSuggestions = DeserializeJson<List<MitigationSuggestionEntry>>(r.MitigationSuggestionsJson)
            ?.Select(m => new RiskMitigationSuggestionDto(
                m.RiskType, m.Suggestion, m.CostImpact, m.Feasibility))
            .ToList();

        return new RiskAnalysisDto(
            OverallScore: r.OverallRiskScore,
            OverallRiskLevel: r.OverallRiskLevel ?? DeriveRiskLevel(r.OverallRiskScore),
            MaxScore: 100,
            RiskBreakdown: new RiskBreakdownDto(flood, seismic, expansiveSoil, liquefaction),
            MitigationSuggestions: mitigationSuggestions,
            VisualizationAssets: assets ?? new RiskVisualizationAssetsDto(null, null));
    }

    private static BoreholeAnalysisDto BuildBoreholeDto(
        BoreholeResult r, BoreholeVisualizationAssetsDto? assets)
    {
        var currency = r.Currency ?? "EGP";

        var placementPoints = DeserializeJson<List<BoreholePlacementPointEntry>>(r.PlacementPointsJson)
            ?.Select(p => new BoreholeAnalysisPlacementPointDto(
                p.Id, p.Latitude, p.Longitude, p.Priority,
                p.Reason, p.EstimatedDepth))
            .ToList();

        return new BoreholeAnalysisDto(
            Recommendation: new BoreholeRecommendationDto(
                r.MinimumRequired, r.OptimalCount, r.CoveragePercentage,
                r.GridSize, r.PlacementStrategy),
            PlacementPoints: placementPoints,
            CostAnalysis: new BoreholeCostAnalysisDto(
                TraditionalApproach: new BoreholeCostApproachDto(
                    r.TraditionalBoreholeCount, r.TraditionalEstimatedCost, currency,
                    "1 borehole per 500 m² (Egyptian standard)"),
                OptimizedApproach: new BoreholeCostApproachDto(
                    r.OptimizedBoreholeCount, r.OptimizedEstimatedCost, currency, null),
                Savings: new BoreholeSavingsDto(
                    r.SavingsAmount, currency, r.SavingsPercentage)),
            VisualizationAssets: assets ?? new BoreholeVisualizationAssetsDto(null));
    }

    // ─── JSON Deserialization Helper ─────────────────────────────────────────

    private static T? DeserializeJson<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<T>(json, JsonOptions); }
        catch (JsonException) { return null; }
    }

    // ─── Risk / Ponding Level Derivation ────────────────────────────────────

    private static string DeriveRiskLevel(int score) => score switch
    {
        <= 20 => "Low",
        <= 40 => "Medium",
        <= 60 => "High",
        _ => "Very High"
    };

    private static string DerivePondingRiskLevel(int? zonesCount, double? totalArea)
    {
        if (zonesCount is null or 0) return "Low";
        if (totalArea > 10000) return "High";
        if (totalArea > 5000) return "Medium";
        return "Low";
    }

    // ─── JSON Entry Types for Deserialization ────────────────────────────────

    private sealed record SlopeDistributionEntry(
        string Category, string Range, double Percentage, string Color);

    private sealed record DepthProfileEntry(
        string Depth, double Sand, double Silt, double Clay, string Type, double? BulkDensity);

    private sealed record FeatureImportanceEntry(
        string Feature, double Weight);

    private sealed record SoilFactorsEntry(
        double ClayContent, double SandContent, double MoistureIndex,
        double DepthToWaterTableMeters, double TerrainSlopePercent);

    private sealed record MitigationSuggestionEntry(
        string RiskType, string Suggestion, string? CostImpact, string? Feasibility);

    private sealed record BoreholePlacementPointEntry(
        string Id, double Latitude, double Longitude, string Priority,
        string? Reason, double? EstimatedDepth);
}
