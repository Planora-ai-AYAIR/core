using Planora.Domain.Shared.Abstractions;

namespace Planora.Domain.Analysis;

public sealed class BoreholeResult : AuditableEntity
{
    public Guid AnalysisJobId { get; private set; }
    public int MinimumRequired { get; private set; }
    public int OptimalCount { get; private set; }
    public double CoveragePercentage { get; private set; }
    public string? GridSize { get; private set; }
    public string? PlacementStrategy { get; private set; }
    public string? PlacementPointsJson { get; private set; }
    public string? PlacementGeoJsonUrl { get; private set; }
    public int TraditionalBoreholeCount { get; private set; }
    public decimal TraditionalEstimatedCost { get; private set; }
    public int OptimizedBoreholeCount { get; private set; }
    public decimal OptimizedEstimatedCost { get; private set; }
    public decimal SavingsAmount { get; private set; }
    public double SavingsPercentage { get; private set; }
    public string? Currency { get; private set; }

    private BoreholeResult() { }

    public BoreholeResult(
        Guid analysisJobId,
        int minimumRequired,
        int optimalCount,
        double coveragePercentage,
        string? gridSize,
        string? placementStrategy,
        string? placementPointsJson,
        string? placementGeoJsonUrl,
        int traditionalBoreholeCount,
        decimal traditionalEstimatedCost,
        int optimizedBoreholeCount,
        decimal optimizedEstimatedCost,
        decimal savingsAmount,
        double savingsPercentage,
        string? currency = "EGP")
    {
        AnalysisJobId = analysisJobId;
        MinimumRequired = minimumRequired;
        OptimalCount = optimalCount;
        CoveragePercentage = coveragePercentage;
        GridSize = gridSize;
        PlacementStrategy = placementStrategy;
        PlacementPointsJson = placementPointsJson;
        PlacementGeoJsonUrl = placementGeoJsonUrl;
        TraditionalBoreholeCount = traditionalBoreholeCount;
        TraditionalEstimatedCost = traditionalEstimatedCost;
        OptimizedBoreholeCount = optimizedBoreholeCount;
        OptimizedEstimatedCost = optimizedEstimatedCost;
        SavingsAmount = savingsAmount;
        SavingsPercentage = savingsPercentage;
        Currency = currency;
    }
}
