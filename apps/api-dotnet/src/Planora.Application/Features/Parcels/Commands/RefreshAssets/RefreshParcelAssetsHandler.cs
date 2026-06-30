using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Parcels.Dtos.RefreshAssets;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Analysis;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.RefreshAssets;

public sealed class RefreshParcelAssetsHandler(
    IParcelRepository parcelRepository,
    IAnalysisJobRepository analysisJobRepository,
    ITopographyResultRepository topographyResultRepository,
    ISoilResultRepository soilResultRepository,
    IRiskResultRepository riskResultRepository,
    IBoreholeResultRepository boreholeResultRepository,
    IStorageService storageService,
    ILogger<RefreshParcelAssetsHandler> logger)
    : IRequestHandler<RefreshParcelAssetsCommand, Result<RefreshAssetsResponse>>
{
    private static readonly TimeSpan UrlExpiry = TimeSpan.FromHours(1);

    public async Task<Result<RefreshAssetsResponse>> Handle(
        RefreshParcelAssetsCommand request,
        CancellationToken ct)
    {
        // 0. Authorization: the parcel must exist and belong to the requesting user.
        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);
        if (parcel is null || parcel.UserId != request.UserId)
        {
            logger.LogWarning(
                "Refresh-assets requested for inaccessible parcel. ParcelId: {ParcelId}, UserId: {UserId}",
                request.ParcelId, request.UserId);
            return ParcelErrors.NotFound;
        }

        // 1. Load completed analysis jobs and the matching module results.
        //    Aggregated flow stores all module results under the single
        //    Aggregated job's Id; fall back to per-module jobs otherwise.
        var jobs = await analysisJobRepository.GetByParcelIdAsync(request.ParcelId, ct);

        var aggregatedJob = FindCompletedJob(jobs, AnalysisType.Aggregated);

        var topographyResult = await LoadResultAsync(
            aggregatedJob ?? FindCompletedJob(jobs, AnalysisType.Topography),
            topographyResultRepository.GetByAnalysisJobIdAsync, ct);
        var soilResult = await LoadResultAsync(
            aggregatedJob ?? FindCompletedJob(jobs, AnalysisType.Soil),
            soilResultRepository.GetByAnalysisJobIdAsync, ct);
        var riskResult = await LoadResultAsync(
            aggregatedJob ?? FindCompletedJob(jobs, AnalysisType.Risk),
            riskResultRepository.GetByAnalysisJobIdAsync, ct);
        var boreholeResult = await LoadResultAsync(
            aggregatedJob ?? FindCompletedJob(jobs, AnalysisType.Borehole),
            boreholeResultRepository.GetByAnalysisJobIdAsync, ct);

        // 2. Re-sign all map-layer keys concurrently. Missing keys resolve to "".
        var contourTask    = PresignAsync(topographyResult?.ContourGeoJsonUrl, ct);
        var pondingTask    = PresignAsync(topographyResult?.PondingGeoJsonUrl, ct);
        var elevationTask  = PresignAsync(topographyResult?.ElevationTileUrl, ct);
        var slopeTask      = PresignAsync(topographyResult?.SlopeTileUrl, ct);
        var soilHeatTask   = PresignAsync(soilResult?.HeatmapTileUrl, ct);
        var soilTypeTask   = PresignAsync(soilResult?.SoilTypeGeoJsonUrl, ct);
        var floodTask      = PresignAsync(riskResult?.FloodGeoJsonUrl, ct);
        var riskHeatTask   = PresignAsync(riskResult?.RiskHeatmapTileUrl, ct);
        var boreholeTask   = PresignAsync(boreholeResult?.PlacementGeoJsonUrl, ct);

        await Task.WhenAll(
            contourTask, pondingTask, elevationTask, slopeTask,
            soilHeatTask, soilTypeTask, floodTask, riskHeatTask, boreholeTask);

        var mapLayers = new RefreshMapLayersDto(
            ContourGeoJsonUrl: await contourTask,
            PondingGeoJsonUrl: await pondingTask,
            ElevationTileUrl: await elevationTask,
            SlopeTileUrl: await slopeTask,
            SoilHeatmapTileUrl: await soilHeatTask,
            SoilTypeGeoJsonUrl: await soilTypeTask,
            FloodRiskZonesGeoJsonUrl: await floodTask,
            RiskHeatmapTileUrl: await riskHeatTask,
            BoreholePointsGeoJsonUrl: await boreholeTask);

        var expireAt = DateTime.UtcNow.Add(UrlExpiry);

        logger.LogInformation(
            "Map-layer presigned URLs refreshed for ParcelId: {ParcelId}. PresignedUrlsExpireAt: {ExpiresAt}",
            request.ParcelId, expireAt);

        return new RefreshAssetsResponse(request.ParcelId, expireAt, mapLayers);
    }

    private static AnalysisJob? FindCompletedJob(IReadOnlyList<AnalysisJob> jobs, AnalysisType type) =>
        jobs.FirstOrDefault(j => j.Type == type && j.Status == AnalysisJobStatus.Completed);

    private static async Task<TResult?> LoadResultAsync<TResult>(
        AnalysisJob? job,
        Func<Guid, CancellationToken, Task<TResult?>> fetch,
        CancellationToken ct)
        where TResult : class
    {
        if (job is null) return null;
        return await fetch(job.Id, ct);
    }

    private async Task<string> PresignAsync(string? s3Key, CancellationToken ct)
    {
        var url = await storageService.TryGetPreSignedUrlAsync(s3Key, UrlExpiry, ct);
        return url ?? string.Empty;
    }
}
