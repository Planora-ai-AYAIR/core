using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record BoreholeResultPayload
{
    [JsonPropertyName("recommendation")]
    public BoreholeRecommendationPayload? Recommendation { get; init; }

    [JsonPropertyName("placementPoints")]
    public List<BoreholePlacementPoint>? PlacementPoints { get; init; }

    [JsonPropertyName("costAnalysis")]
    public BoreholeCostAnalysisPayload? CostAnalysis { get; init; }

    [JsonPropertyName("visualizationAssets")]
    public BoreholeAssetsPayload? VisualizationAssets { get; init; }
}

public sealed record BoreholeRecommendationPayload
{
    [JsonPropertyName("minimumRequired")]
    public int MinimumRequired { get; init; }

    [JsonPropertyName("optimalCount")]
    public int OptimalCount { get; init; }

    [JsonPropertyName("coveragePercentage")]
    public double CoveragePercentage { get; init; }

    [JsonPropertyName("gridSize")]
    public string? GridSize { get; init; }

    [JsonPropertyName("strategy")]
    public string? Strategy { get; init; }
}

public sealed record BoreholeCostAnalysisPayload
{
    [JsonPropertyName("traditionalApproach")]
    public BoreholeCostOptionPayload? TraditionalApproach { get; init; }

    [JsonPropertyName("optimizedApproach")]
    public BoreholeCostOptionPayload? OptimizedApproach { get; init; }

    [JsonPropertyName("savings")]
    public BoreholeSavingsPayload? Savings { get; init; }
}

public sealed record BoreholeCostOptionPayload
{
    [JsonPropertyName("boreholes")]
    public int Boreholes { get; init; }

    [JsonPropertyName("estimatedCost")]
    public decimal EstimatedCost { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("basis")]
    public string? Basis { get; init; }
}

public sealed record BoreholeSavingsPayload
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("percentage")]
    public double Percentage { get; init; }
}

public sealed record BoreholeAssetsPayload
{
    [JsonPropertyName("boreholePointsGeoJsonUrl")]
    public string? BoreholePointsGeoJsonUrl { get; init; }
}
