using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Options;
using Planora.Application.Features.Parcels.Dtos.Analysis;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
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

        // 1. Redis cache
        var cached = await cacheService.GetAsync<ParcelAnalysisResponse>(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for parcel analysis assets. ParcelId: {ParcelId}", request.ParcelId);
            return cached;
        }

        logger.LogInformation("Generating presigned asset URLs for ParcelId: {ParcelId}", request.ParcelId);

        // 2. Load all completed analysis jobs for this parcel in one query
        var jobs = await analysisJobRepository.GetByParcelIdAsync(request.ParcelId, ct);

        // 3. Build each module section concurrently
        var topographyTask = BuildTopographyAssetsAsync(jobs, ct);
        var soilTask       = BuildSoilAssetsAsync(jobs, ct);
        var riskTask       = BuildRiskAssetsAsync(jobs, ct);
        var boreholeTask   = BuildBoreholeAssetsAsync(jobs, ct);

        await Task.WhenAll(topographyTask, soilTask, riskTask, boreholeTask);

        var expireAt = DateTime.UtcNow.Add(UrlExpiry);

        var response = new ParcelAnalysisResponse(
            ParcelId:             request.ParcelId,
            Topography:           topographyTask.Result,
            Soil:                 soilTask.Result,
            Risk:                 riskTask.Result,
            Borehole:             boreholeTask.Result,
            PresignedUrlsExpireAt: expireAt);

        // 4. Cache for 50 minutes
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

    private async Task<TopographyAssetsDto?> BuildTopographyAssetsAsync(IReadOnlyList<Domain.AnalysisJob.AnalysisJob> jobs, CancellationToken ct)
    {
        var job = jobs.FirstOrDefault(j =>
            j.Type == AnalysisType.Topography && j.Status == AnalysisJobStatus.Completed);

        if (job is null) return null;

        var result = await topographyResultRepository.GetByAnalysisJobIdAsync(job.Id, ct);
        if (result is null) return null;

        var contour   = await storageService.TryGetPreSignedUrlAsync(result.ContourGeoJsonUrl,   UrlExpiry, ct);
        var ponding   = await storageService.TryGetPreSignedUrlAsync(result.PondingGeoJsonUrl,   UrlExpiry, ct);
        var elevation = await storageService.TryGetPreSignedUrlAsync(result.ElevationTileUrl,    UrlExpiry, ct);
        var slope     = await storageService.TryGetPreSignedUrlAsync(result.SlopeTileUrl,        UrlExpiry, ct);

        return new TopographyAssetsDto(contour, ponding, elevation, slope);
    }

    private async Task<SoilAssetsDto?> BuildSoilAssetsAsync(IReadOnlyList<Domain.AnalysisJob.AnalysisJob> jobs, CancellationToken ct)
    {
        var job = jobs.FirstOrDefault(j =>
            j.Type == AnalysisType.Soil && j.Status == AnalysisJobStatus.Completed);

        if (job is null) return null;

        var result = await soilResultRepository.GetByAnalysisJobIdAsync(job.Id, ct);
        if (result is null) return null;

        var heatmap = await storageService.TryGetPreSignedUrlAsync(result.HeatmapTileUrl, UrlExpiry, ct);

        return new SoilAssetsDto(heatmap);
    }

    private async Task<RiskAssetsDto?> BuildRiskAssetsAsync(IReadOnlyList<Domain.AnalysisJob.AnalysisJob> jobs, CancellationToken ct)
    {
        var job = jobs.FirstOrDefault(j =>
            j.Type == AnalysisType.Risk && j.Status == AnalysisJobStatus.Completed);

        if (job is null) return null;

        var result = await riskResultRepository.GetByAnalysisJobIdAsync(job.Id, ct);
        if (result is null) return null;

        var flood = await storageService.TryGetPreSignedUrlAsync(result.FloodGeoJsonUrl, UrlExpiry, ct);

        return new RiskAssetsDto(flood);
    }

    private async Task<BoreholeAssetsDto?> BuildBoreholeAssetsAsync(IReadOnlyList<Domain.AnalysisJob.AnalysisJob> jobs, CancellationToken ct)
    {
        var job = jobs.FirstOrDefault(j =>
            j.Type == AnalysisType.Borehole && j.Status == AnalysisJobStatus.Completed);

        if (job is null) return null;

        var result = await boreholeResultRepository.GetByAnalysisJobIdAsync(job.Id, ct);
        if (result is null) return null;

        var placement = await storageService.TryGetPreSignedUrlAsync(result.PlacementGeoJsonUrl, UrlExpiry, ct);

        return new BoreholeAssetsDto(placement);
    }
}
