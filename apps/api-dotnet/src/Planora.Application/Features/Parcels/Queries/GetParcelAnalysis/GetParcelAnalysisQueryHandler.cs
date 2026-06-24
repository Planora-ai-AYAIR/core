using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Options;
using Planora.Application.Features.Parcels.Dtos.Analysis;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Analysis;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetParcelAnalysis;

public sealed class GetParcelAnalysisQueryHandler(
    IAnalysisJobRepository analysisJobRepository,
    ITopographyResultRepository topographyResultRepository,
    ISoilResultRepository soilResultRepository,
    IRiskResultRepository riskResultRepository,
    IBoreholeResultRepository boreholeResultRepository,
    IStorageService storageService,
    IHybridCacheService cacheService,
    ILogger<GetParcelAnalysisQueryHandler> logger)
    : IRequestHandler<GetParcelAnalysisQuery, Result<ParcelAnalysisResponse>>
{
    private static readonly TimeSpan UrlExpiry   = TimeSpan.FromHours(1);
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(50);

    public async Task<Result<ParcelAnalysisResponse>> Handle(
        GetParcelAnalysisQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"analysis:{request.ParcelId}";

        // Redis cache
        var cached = await cacheService.GetAsync<ParcelAnalysisResponse>(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for parcel analysis assets. ParcelId: {ParcelId}", request.ParcelId);
            return cached;
        }

        logger.LogInformation("Generating presigned asset URLs for ParcelId: {ParcelId}", request.ParcelId);

        // Load all completed analysis jobs for this parcel in one query
        var jobs = await analysisJobRepository.GetByParcelIdAsync(request.ParcelId, ct);

        // Read each module's persisted result SEQUENTIALLY.
        var topographyResult = await GetResultAsync(jobs, AnalysisType.Topography, topographyResultRepository.GetByAnalysisJobIdAsync, ct);
        var soilResult       = await GetResultAsync(jobs, AnalysisType.Soil,       soilResultRepository.GetByAnalysisJobIdAsync,       ct);
        var riskResult       = await GetResultAsync(jobs, AnalysisType.Risk,       riskResultRepository.GetByAnalysisJobIdAsync,       ct);
        var boreholeResult   = await GetResultAsync(jobs, AnalysisType.Borehole,   boreholeResultRepository.GetByAnalysisJobIdAsync,   ct);

        // 4. Generate presigned URLs CONCURRENTLY
        var topographyTask = BuildTopographyAssetsAsync(topographyResult, ct);
        var soilTask       = BuildSoilAssetsAsync(soilResult, ct);
        var riskTask       = BuildRiskAssetsAsync(riskResult, ct);
        var boreholeTask   = BuildBoreholeAssetsAsync(boreholeResult, ct);

        await Task.WhenAll(topographyTask, soilTask, riskTask, boreholeTask);

        var expireAt = DateTime.UtcNow.Add(UrlExpiry);

        var response = new ParcelAnalysisResponse(
            ParcelId:             request.ParcelId,
            Topography:           topographyTask.Result,
            Soil:                 soilTask.Result,
            Risk:                 riskTask.Result,
            Borehole:             boreholeTask.Result,
            PresignedUrlsExpireAt: expireAt);

        // Cache for 50 minutes
        await cacheService.SetAsync(cacheKey, response, new CacheEntryOptions
        {
            Expiration            = CacheExpiry,
            LocalCacheExpiration  = TimeSpan.FromMinutes(5),
            Tags                  = ["analysis", $"parcel:{request.ParcelId}"]
        }, ct);

        logger.LogInformation(
            "Presigned asset URLs generated and cached for ParcelId: {ParcelId}. ExpiresAt: {ExpiresAt}",
            request.ParcelId, expireAt);

        return response;
    }

    // ─── Private helpers ──────────────────────────────────────────────────────
    private static async Task<TResult?> GetResultAsync<TResult>(IReadOnlyList<AnalysisJob> jobs, AnalysisType type, Func<Guid, CancellationToken, Task<TResult?>> fetch, CancellationToken ct)
        where TResult : class
    {
        var job = jobs.FirstOrDefault(j => j.Type == type && j.Status == AnalysisJobStatus.Completed);
        if (job is null) return null;

        return await fetch(job.Id, ct);
    }

    private async Task<TopographyAssetsDto?> BuildTopographyAssetsAsync(TopographyResult? result, CancellationToken ct)
    {
        if (result is null) return null;

        var contour   = await storageService.TryGetPreSignedUrlAsync(result.ContourGeoJsonUrl,   UrlExpiry, ct);
        var ponding   = await storageService.TryGetPreSignedUrlAsync(result.PondingGeoJsonUrl,   UrlExpiry, ct);
        var elevation = await storageService.TryGetPreSignedUrlAsync(result.ElevationTileUrl,    UrlExpiry, ct);
        var slope     = await storageService.TryGetPreSignedUrlAsync(result.SlopeTileUrl,        UrlExpiry, ct);

        return new TopographyAssetsDto(contour, ponding, elevation, slope);
    }

    private async Task<SoilAssetsDto?> BuildSoilAssetsAsync(SoilResult? result, CancellationToken ct)
    {
        if (result is null) return null;

        var heatmap = await storageService.TryGetPreSignedUrlAsync(result.HeatmapTileUrl, UrlExpiry, ct);

        return new SoilAssetsDto(heatmap);
    }

    private async Task<RiskAssetsDto?> BuildRiskAssetsAsync(RiskResult? result, CancellationToken ct)
    {
        if (result is null) return null;

        var flood = await storageService.TryGetPreSignedUrlAsync(result.FloodGeoJsonUrl, UrlExpiry, ct);

        return new RiskAssetsDto(flood);
    }

    private async Task<BoreholeAssetsDto?> BuildBoreholeAssetsAsync(BoreholeResult? result, CancellationToken ct)
    {
        if (result is null) return null;

        var placement = await storageService.TryGetPreSignedUrlAsync(result.PlacementGeoJsonUrl, UrlExpiry, ct);

        return new BoreholeAssetsDto(placement);
    }
}
