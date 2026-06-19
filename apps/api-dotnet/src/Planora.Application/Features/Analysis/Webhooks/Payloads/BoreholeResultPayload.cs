using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Webhooks.Payloads;

public sealed record BoreholeResultPayload
{
    [JsonPropertyName("pythonJobId")]
    public string PythonJobId { get; init; } = string.Empty;

    [JsonPropertyName("minimumRequired")]
    public int MinimumRequired { get; init; }

    [JsonPropertyName("optimalCount")]
    public int OptimalCount { get; init; }

    [JsonPropertyName("coveragePercentage")]
    public double CoveragePercentage { get; init; }

    [JsonPropertyName("gridSize")]
    public string? GridSize { get; init; }

    [JsonPropertyName("placementStrategy")]
    public string? PlacementStrategy { get; init; }

    [JsonPropertyName("placementPoints")]
    public List<BoreholePlacementPoint>? PlacementPoints { get; init; }

    [JsonPropertyName("placementGeoJsonUrl")]
    public string? PlacementGeoJsonUrl { get; init; }

    [JsonPropertyName("traditionalBoreholeCount")]
    public int TraditionalBoreholeCount { get; init; }

    [JsonPropertyName("traditionalEstimatedCost")]
    public decimal TraditionalEstimatedCost { get; init; }

    [JsonPropertyName("optimizedBoreholeCount")]
    public int OptimizedBoreholeCount { get; init; }

    [JsonPropertyName("optimizedEstimatedCost")]
    public decimal OptimizedEstimatedCost { get; init; }

    [JsonPropertyName("savingsAmount")]
    public decimal SavingsAmount { get; init; }

    [JsonPropertyName("savingsPercentage")]
    public double SavingsPercentage { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }
}
