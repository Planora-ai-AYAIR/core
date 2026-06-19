using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Options;
using Planora.Application.Features.Parcels.Dtos.BoreholeResults;
using Planora.Application.Features.Parcels.Errors;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;
using System.Text.Json;

namespace Planora.Application.Features.Parcels.Queries.GetBoreholeResults;

public sealed class GetBoreholeResultsQueryHandler(
    IAnalysisJobRepository analysisJobRepository,
    IBoreholeResultRepository boreholeResultRepository,
    IHybridCacheService cacheService,
    ILogger<GetBoreholeResultsQueryHandler> logger)
    : IRequestHandler<GetBoreholeResultsQuery, Result<BoreholeResultsResponse>>
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(50);

    public async Task<Result<BoreholeResultsResponse>> Handle(GetBoreholeResultsQuery request, CancellationToken ct)
    {
        var cacheKey = $"borehole:{request.ParcelId}";

        var cached = await cacheService.GetAsync<BoreholeResultsResponse>(cacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        var jobs = await analysisJobRepository.GetByParcelIdAsync(request.ParcelId, ct);
        var boreholeJob = jobs.FirstOrDefault(j => j.Type == AnalysisType.Borehole && j.Status == AnalysisJobStatus.Completed);

        if (boreholeJob is null)
        {
            return ReportErrors.NotReady;
        }

        var boreholeResult = await boreholeResultRepository.GetByAnalysisJobIdAsync(boreholeJob.Id, ct);

        if (boreholeResult is null)
        {
            return ReportErrors.MetadataCorrupted;
        }

        var placementPoints = !string.IsNullOrWhiteSpace(boreholeResult.PlacementPointsJson)
            ? JsonSerializer.Deserialize<List<BoreholePlacementPointDto>>(boreholeResult.PlacementPointsJson)
            : null;

        var response = new BoreholeResultsResponse(
            request.ParcelId,
            boreholeResult.MinimumRequired,
            boreholeResult.OptimalCount,
            boreholeResult.CoveragePercentage,
            boreholeResult.GridSize,
            boreholeResult.PlacementStrategy,
            placementPoints,
            boreholeResult.PlacementGeoJsonUrl,
            new CostComparisonDto(
                boreholeResult.TraditionalBoreholeCount,
                boreholeResult.TraditionalEstimatedCost,
                boreholeResult.OptimizedBoreholeCount,
                boreholeResult.OptimizedEstimatedCost,
                boreholeResult.SavingsAmount,
                boreholeResult.SavingsPercentage,
                boreholeResult.Currency ?? "EGP"),
            boreholeResult.CreatedAt
        );

        await cacheService.SetAsync(cacheKey, response, new CacheEntryOptions
        {
            Expiration = CacheExpiry,
            LocalCacheExpiration = TimeSpan.FromMinutes(5),
            Tags = ["borehole", $"parcel:{request.ParcelId}"]
        }, ct);

        return response;
    }
}
