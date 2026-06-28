// Planora.Infrastructure/Persistence/Queries/AnalysisResultQuery.cs
using Microsoft.EntityFrameworkCore;
using Planora.Application.Features.Reports.Dtos;
using Planora.Application.Features.Reports.Dtos.SubmitPdfJob;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Analysis;
using Planora.Domain.AnalysisJob;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Persistence.Repositories;

public sealed class AnalysisResultQuery(PlanoraDbContext context) : IAnalysisResultQuery
{
    public async Task<AggregatedAnalysisData?> GetByParcelIdAsync(Guid parcelId, CancellationToken ct)
    {
        var job = await context.AnalysisJobs
            .AsNoTracking()
            .Where(j => j.ParcelId == parcelId && j.Status == AnalysisJobStatus.Completed)
            .OrderByDescending(j => j.CompletedAt)
            .FirstOrDefaultAsync(ct);

        if (job is null) return null;

        var topography = await context.Set<TopographyResult>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.AnalysisJobId == job.Id, ct);

        var soil = await context.Set<SoilResult>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.AnalysisJobId == job.Id, ct);

        var risk = await context.Set<RiskResult>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AnalysisJobId == job.Id, ct);

        var borehole = await context.Set<BoreholeResult>()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.AnalysisJobId == job.Id, ct);

        var bearing = await context.Set<BearingResult>()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.AnalysisJobId == job.Id, ct);

        return new AggregatedAnalysisData
        {
            AnalysisJobId = job.Id,
            Topography = topography == null ? null : new PdfTopographyData
            {
                ElevationMin = topography.ElevationMin,
                ElevationMax = topography.ElevationMax,
                ElevationMean = topography.ElevationMean,
                SlopeDistributionJson = topography.SlopeDistributionJson,
                CutVolume = topography.CutVolume,
                FillVolume = topography.FillVolume,
                NetVolume = topography.NetVolume,
                ContourInterval = topography.ContourInterval,
                ContourGeoJsonUrl = topography.ContourGeoJsonUrl,
                PondingGeoJsonUrl = topography.PondingGeoJsonUrl,
                PondingZonesCount = topography.PondingZonesCount,
                PondingTotalArea = topography.PondingTotalArea,
                ElevationTileUrl = topography.ElevationTileUrl,
                SlopeTileUrl = topography.SlopeTileUrl
            },
            Soil = soil == null ? null : new PdfSoilData
            {
                SandPercent = soil.SandPercent,
                SiltPercent = soil.SiltPercent,
                ClayPercent = soil.ClayPercent,
                BulkDensity = soil.BulkDensity,
                OrganicCarbon = soil.OrganicCarbon,
                Ph = soil.Ph,
                PrimaryType = soil.PrimaryType,
                UsdaClass = soil.UsdaClass,
                AiConfidence = soil.AiConfidence,
                MultiDepthProfileJson = soil.MultiDepthProfileJson,
                HeatmapTileUrl = soil.HeatmapTileUrl
            },
            Bearing = bearing == null ? null : new PdfBearingData
            {
                BearingCapacityKpa = bearing.BearingCapacityKpa,
                Classification = bearing.Classification
            },
            Risk = risk == null ? null : new PdfRiskData
            {
                FloodRiskScore = risk.FloodRiskScore,
                SeismicRiskScore = risk.SeismicRiskScore,
                ExpansiveSoilRisk = risk.ExpansiveSoilRisk,
                LiquefactionRisk = risk.LiquefactionRisk,
                OverallRiskScore = risk.OverallRiskScore,
                OverallRiskLevel = risk.OverallRiskLevel,
                FloodGeoJsonUrl = risk.FloodGeoJsonUrl,
                SeismicLevel = risk.SeismicLevel,
                SeismicSource = risk.SeismicSource,
                ExpansiveSoilLevel = risk.ExpansiveSoilLevel,
                ReplacementDepth = risk.ReplacementDepth,
                LiquefactionLevel = risk.LiquefactionLevel
            },
            Borehole = borehole == null ? null : new PdfBoreholeData
            {
                MinimumRequired = borehole.MinimumRequired,
                OptimalCount = borehole.OptimalCount,
                CoveragePercentage = borehole.CoveragePercentage,
                GridSize = borehole.GridSize,
                PlacementStrategy = borehole.PlacementStrategy,
                PlacementPointsJson = borehole.PlacementPointsJson,
                PlacementGeoJsonUrl = borehole.PlacementGeoJsonUrl,
                TraditionalBoreholeCount = borehole.TraditionalBoreholeCount,
                TraditionalEstimatedCost = borehole.TraditionalEstimatedCost,
                OptimizedBoreholeCount = borehole.OptimizedBoreholeCount,
                OptimizedEstimatedCost = borehole.OptimizedEstimatedCost,
                SavingsAmount = borehole.SavingsAmount,
                SavingsPercentage = borehole.SavingsPercentage
            }
        };
    }
}