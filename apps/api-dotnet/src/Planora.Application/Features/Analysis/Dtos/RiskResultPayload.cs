using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record RiskResultPayload
{
    [JsonPropertyName("overallScore")]
    public int OverallScore { get; init; }

    [JsonPropertyName("overallRiskLevel")]
    public string? OverallRiskLevel { get; init; }

    [JsonPropertyName("maxScore")]
    public int? MaxScore { get; init; }

    [JsonPropertyName("riskBreakdown")]
    public RiskBreakdownPayload? RiskBreakdown { get; init; }

    [JsonPropertyName("visualizationAssets")]
    public RiskAssetsPayload? VisualizationAssets { get; init; }

    [JsonPropertyName("mitigationSuggestions")]
    public List<RiskMitigationSuggestionPayload>? MitigationSuggestions { get; init; }
}

public sealed record RiskBreakdownPayload
{
    [JsonPropertyName("flood")]
    public RiskSubResultPayload? Flood { get; init; }

    [JsonPropertyName("seismic")]
    public RiskSubResultPayload? Seismic { get; init; }

    [JsonPropertyName("expansiveSoil")]
    public RiskSubResultPayload? ExpansiveSoil { get; init; }

    [JsonPropertyName("liquefaction")]
    public RiskSubResultPayload? Liquefaction { get; init; }
}

public sealed record RiskAssetsPayload
{
    [JsonPropertyName("floodRiskZonesGeoJsonUrl")]
    public string? FloodRiskZonesGeoJsonUrl { get; init; }

    [JsonPropertyName("riskHeatmapTileUrl")]
    public string? RiskHeatmapTileUrl { get; init; }
}

public sealed record RiskMitigationSuggestionPayload
{
    [JsonPropertyName("riskType")]
    public string RiskType { get; init; } = string.Empty;

    [JsonPropertyName("suggestion")]
    public string Suggestion { get; init; } = string.Empty;

    [JsonPropertyName("costImpact")]
    public string? CostImpact { get; init; }

    [JsonPropertyName("feasibility")]
    public string? Feasibility { get; init; }
}
