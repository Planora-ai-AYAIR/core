using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Options;
using Planora.Application.Features.Parcels.Dtos.SoilResults;
using Planora.Application.Features.Parcels.Errors;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;
using System.Text.Json;

namespace Planora.Application.Features.Parcels.Queries.GetSoilResults;

public sealed class GetSoilResultsQueryHandler(
    IAnalysisJobRepository analysisJobRepository,
    ISoilResultRepository soilResultRepository,
    IHybridCacheService cacheService,
    ILogger<GetSoilResultsQueryHandler> logger)
    : IRequestHandler<GetSoilResultsQuery, Result<SoilResultsResponse>>
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(50);

    public async Task<Result<SoilResultsResponse>> Handle(GetSoilResultsQuery request, CancellationToken ct)
    {
        var cacheKey = $"soil:{request.ParcelId}:depth={request.Depth}";

        var cached = await cacheService.GetAsync<SoilResultsResponse>(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for soil results. ParcelId: {ParcelId}", request.ParcelId);
            return cached;
        }

        logger.LogInformation("Fetching soil results for ParcelId: {ParcelId}", request.ParcelId);

        // Find the completed soil analysis job for this parcel
        var jobs = await analysisJobRepository.GetByParcelIdAsync(request.ParcelId, ct);
        var soilJob = jobs.FirstOrDefault(j => j.Type == AnalysisType.Soil && j.Status == AnalysisJobStatus.Completed);

        if (soilJob is null)
        {
            logger.LogWarning("No completed soil analysis job found for ParcelId: {ParcelId}", request.ParcelId);
            return ReportErrors.NotReady;
        }

        var soilResult = await soilResultRepository.GetByAnalysisJobIdAsync(soilJob.Id, ct);

        if (soilResult is null)
        {
            logger.LogError("Soil result entity missing for AnalysisJobId: {AnalysisJobId}, ParcelId: {ParcelId}", soilJob.Id, request.ParcelId);
            return ReportErrors.MetadataCorrupted;
        }

        var multiDepthProfile = !string.IsNullOrWhiteSpace(soilResult.MultiDepthProfileJson)
            ? JsonSerializer.Deserialize<List<DepthProfileItem>>(soilResult.MultiDepthProfileJson)
            : null;

        var response = new SoilResultsResponse(
            request.ParcelId,
            soilResult.SandPercent,
            soilResult.SiltPercent,
            soilResult.ClayPercent,
            soilResult.CompositionUnit ?? "%",
            soilResult.BulkDensity,
            soilResult.BulkDensityUnit ?? "g/cm³",
            soilResult.OrganicCarbon,
            soilResult.OrganicCarbonUnit ?? "%",
            soilResult.Ph,
            soilResult.PrimaryType ?? soilResult.BearingCapacityCategory,
            soilResult.UsdaClass ?? "",
            soilResult.AiConfidence,
            multiDepthProfile,
            soilResult.HeatmapTileUrl,
            soilResult.CreatedAt
        );

        await cacheService.SetAsync(cacheKey, response, new CacheEntryOptions
        {
            Expiration = CacheExpiry,
            LocalCacheExpiration = TimeSpan.FromMinutes(5),
            Tags = ["soil", $"parcel:{request.ParcelId}"]
        }, ct);

        logger.LogInformation("Soil results assembled and cached for ParcelId: {ParcelId}", request.ParcelId);

        return response;
    }
}
