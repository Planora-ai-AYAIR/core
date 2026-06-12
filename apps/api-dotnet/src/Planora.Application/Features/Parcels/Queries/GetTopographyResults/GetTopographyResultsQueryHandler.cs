using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Options;
using Planora.Application.Features.Parcels.Dtos.TopographyResults;
using Planora.Application.Features.Parcels.Errors;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Enums;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetTopographyResults;

public sealed class GetTopographyResultsQueryHandler(
    IReportRepository reportRepository,
    IStorageService storageService,
    IHybridCacheService cacheService,
    ILogger<GetTopographyResultsQueryHandler> logger)
    : IRequestHandler<GetTopographyResultsQuery, Result<TopographyResultsResponse>>
{
    private static readonly TimeSpan UrlExpiry = TimeSpan.FromHours(1);
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(50);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Result<TopographyResultsResponse>> Handle(
        GetTopographyResultsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"topography:{request.ParcelId}:tiles={request.IncludeTiles}";

        // 1. Fast path: Redis Cache
        var cached = await cacheService.GetAsync<TopographyResultsResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for topography results. ParcelId: {ParcelId}", request.ParcelId);
            return cached;
        }

        // 2. Fetch from database
        var report = await reportRepository.GetByParcelIdWithDetailsAsync(request.ParcelId, cancellationToken);

        if (report is null)
        {
            logger.LogWarning("No report found for ParcelId: {ParcelId}", request.ParcelId);
            return ReportErrors.NotFound;
        }

        if (report.Status != ReportStatus.Completed)
        {
            logger.LogInformation(
                "Report for ParcelId {ParcelId} is not ready yet. Status: {Status}",
                request.ParcelId, report.Status);
            return ReportErrors.NotReady;
        }

        // 3. Extract the Topographic module
        var topoModule = report.Modules.FirstOrDefault(m => m.ModuleType == ModuleType.Topographic);

        if (topoModule is null || string.IsNullOrWhiteSpace(topoModule.OutputMetadata))
        {
            logger.LogError("Topography module missing or has no metadata. ReportId: {ReportId}", report.Id);
            return ReportErrors.TopographyModuleMissing;
        }

        // 4. Deserialize JSON metadata
        TopographyJsonMetadata? meta;
        try
        {
            meta = JsonSerializer.Deserialize<TopographyJsonMetadata>(topoModule.OutputMetadata, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize topography metadata. ReportId: {ReportId}", report.Id);
            return ReportErrors.MetadataCorrupted;
        }

        if (meta is null)
        {
            logger.LogError("Deserialized metadata was null. ReportId: {ReportId}", report.Id);
            return ReportErrors.MetadataCorrupted;
        }

        // 5. Generate Pre-signed S3 URLs
        // GeoJSON URLs are always included
        var contourUrl = meta.ContourS3Key is not null
            ? await storageService.GetPreSignedUrlAsync(meta.ContourS3Key, UrlExpiry)
            : string.Empty;

        var pondingUrl = meta.PondingS3Key is not null
            ? await storageService.GetPreSignedUrlAsync(meta.PondingS3Key, UrlExpiry)
            : string.Empty;

        // Raster tile URLs are optional — only when includeTiles=true
        RasterTilesDto? rasterTiles = null;
        if (request.IncludeTiles && meta.ElevationRasterS3Key is not null && meta.SlopeRasterS3Key is not null)
        {
            var elevationUrl = await storageService.GetPreSignedUrlAsync(meta.ElevationRasterS3Key, UrlExpiry);
            var slopeUrl = await storageService.GetPreSignedUrlAsync(meta.SlopeRasterS3Key, UrlExpiry);
            rasterTiles = new RasterTilesDto(elevationUrl, slopeUrl);
        }

        // 6. Assemble the response
        var response = new TopographyResultsResponse(
            ParcelId: request.ParcelId,
            Elevation: new ElevationDto(meta.ElevationMin, meta.ElevationMax, meta.ElevationMean, "m"),
            SlopeAnalysis: new SlopeAnalysisDto(
                meta.SlopeDistribution
                    .Select(s => new SlopeCategoryDto(s.Category, s.Range, s.Percentage, s.Color))
                    .ToList()),
            CutFill: new CutFillDto(meta.CutVolume, meta.FillVolume, meta.NetVolume, "m\u00b3"),
            ContourLines: new ContourLinesDto(contourUrl, meta.ContourInterval),
            PondingRisk: new PondingRiskDto(meta.PondingZonesCount, meta.PondingTotalArea, "m\u00b2", pondingUrl),
            RasterTiles: rasterTiles,
            GeneratedAt: meta.GeneratedAt
        );

        // 7. Store in cache for next requests
        await cacheService.SetAsync(
            cacheKey,
            response,
            new CacheEntryOptions
            {
                Expiration = CacheExpiry,
                LocalCacheExpiration = TimeSpan.FromMinutes(5),
                Tags = ["topography", $"parcel:{request.ParcelId}"]
            },
            cancellationToken);

        logger.LogInformation(
            "Topography results assembled and cached for ParcelId: {ParcelId}",
            request.ParcelId);

        return response;
    }
}
