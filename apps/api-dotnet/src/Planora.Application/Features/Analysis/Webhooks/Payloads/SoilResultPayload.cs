using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Webhooks.Payloads;

public sealed record SoilResultPayload
{
    [JsonPropertyName("pythonJobId")]
    public string PythonJobId { get; init; } = string.Empty;

    [JsonPropertyName("sandPercent")]
    public double SandPercent { get; init; }

    [JsonPropertyName("siltPercent")]
    public double SiltPercent { get; init; }

    [JsonPropertyName("clayPercent")]
    public double ClayPercent { get; init; }

    [JsonPropertyName("bulkDensity")]
    public double BulkDensity { get; init; }

    [JsonPropertyName("organicCarbon")]
    public double OrganicCarbon { get; init; }

    [JsonPropertyName("ph")]
    public double Ph { get; init; }

    [JsonPropertyName("bearingCapacityEstimate")]
    public double BearingCapacityEstimate { get; init; }

    [JsonPropertyName("bearingCapacityCategory")]
    public string BearingCapacityCategory { get; init; } = string.Empty;

    [JsonPropertyName("compositionUnit")]
    public string? CompositionUnit { get; init; }

    [JsonPropertyName("bulkDensityUnit")]
    public string? BulkDensityUnit { get; init; }

    [JsonPropertyName("organicCarbonUnit")]
    public string? OrganicCarbonUnit { get; init; }

    [JsonPropertyName("primaryType")]
    public string? PrimaryType { get; init; }

    [JsonPropertyName("usdaClass")]
    public string? UsdaClass { get; init; }

    [JsonPropertyName("aiConfidence")]
    public double? AiConfidence { get; init; }

    [JsonPropertyName("depthProfiles")]
    public List<DepthProfileEntry>? DepthProfiles { get; init; }

    [JsonPropertyName("heatmapTileUrl")]
    public string? HeatmapTileUrl { get; init; }
}
