using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Options;
using Planora.Application.Features.Parcels.Errors;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;
using System.Text.Json;

namespace Planora.Application.Features.Parcels.Queries.GetRiskResults;

public sealed class GetRiskResultsQueryHandler(
    IAnalysisJobRepository analysisJobRepository,
    IRiskResultRepository riskResultRepository,
    IHybridCacheService cacheService,
    ILogger<GetRiskResultsQueryHandler> logger)
    : IRequestHandler<GetRiskResultsQuery, Result<RiskResultsResponse>>
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(50);

    public async Task<Result<RiskResultsResponse>> Handle(GetRiskResultsQuery request, CancellationToken ct)
    {
        var cacheKey = $"risk:{request.ParcelId}";

        var cached = await cacheService.GetAsync<RiskResultsResponse>(cacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        var jobs = await analysisJobRepository.GetByParcelIdAsync(request.ParcelId, ct);
        var riskJob = jobs.FirstOrDefault(j => j.Type == AnalysisType.Risk && j.Status == AnalysisJobStatus.Completed);

        if (riskJob is null)
        {
            return ReportErrors.NotReady;
        }

        var riskResult = await riskResultRepository.GetByAnalysisJobIdAsync(riskJob.Id, ct);

        if (riskResult is null)
        {
            return ReportErrors.MetadataCorrupted;
        }

        var response = new RiskResultsResponse(
            request.ParcelId,
            riskResult.OverallRiskScore,
            riskResult.OverallRiskLevel ?? DeriveRiskLevel(riskResult.OverallRiskScore),
            new RiskSubResultDto(
                riskResult.FloodRiskScore,
                riskResult.FloodLevel ?? DeriveRiskLevel(riskResult.FloodRiskScore),
                DeserializeFactors(riskResult.FloodFactorsJson),
                riskResult.FloodGeoJsonUrl),
            new RiskSubResultDto(
                riskResult.SeismicRiskScore,
                riskResult.SeismicLevel ?? DeriveRiskLevel(riskResult.SeismicRiskScore),
                DeserializeFactors(riskResult.SeismicFactorsJson),
                null,
                riskResult.SeismicSource),
            new RiskSubResultDto(
                riskResult.ExpansiveSoilRisk,
                riskResult.ExpansiveSoilLevel ?? DeriveRiskLevel(riskResult.ExpansiveSoilRisk),
                DeserializeFactors(riskResult.ExpansiveSoilFactorsJson),
                null,
                null,
                riskResult.ReplacementDepth),
            new RiskSubResultDto(
                riskResult.LiquefactionRisk,
                riskResult.LiquefactionLevel ?? DeriveRiskLevel(riskResult.LiquefactionRisk),
                DeserializeFactors(riskResult.LiquefactionFactorsJson),
                null,
                null,
                null,
                riskResult.LiquefactionSusceptibility),
            riskResult.CreatedAt
        );

        await cacheService.SetAsync(cacheKey, response, new CacheEntryOptions
        {
            Expiration = CacheExpiry,
            LocalCacheExpiration = TimeSpan.FromMinutes(5),
            Tags = ["risk", $"parcel:{request.ParcelId}"]
        }, ct);

        return response;
    }

    private static string DeriveRiskLevel(int score) => score switch
    {
        <= 20 => "Very Low",
        <= 40 => "Low",
        <= 60 => "Moderate",
        <= 80 => "High",
        _ => "Very High"
    };

    private static List<string>? DeserializeFactors(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<List<string>>(json);
}

public sealed record RiskResultsResponse(
    Guid ParcelId,
    int OverallRiskScore,
    string OverallRiskLevel,
    RiskSubResultDto Flood,
    RiskSubResultDto Seismic,
    RiskSubResultDto ExpansiveSoil,
    RiskSubResultDto Liquefaction,
    DateTime GeneratedAt
);

public sealed record RiskSubResultDto(
    int Score,
    string Level,
    List<string>? Factors = null,
    string? GeoJsonUrl = null,
    string? Source = null,
    double? ReplacementDepth = null,
    string? Susceptibility = null
);
