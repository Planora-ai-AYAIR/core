using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record RiskResultPayload
{
    [JsonPropertyName("pythonJobId")]
    public string PythonJobId { get; init; } = string.Empty;

    [JsonPropertyName("floodRiskScore")]
    public int FloodRiskScore { get; init; }

    [JsonPropertyName("seismicRiskScore")]
    public int SeismicRiskScore { get; init; }

    [JsonPropertyName("expansiveSoilRisk")]
    public int ExpansiveSoilRisk { get; init; }

    [JsonPropertyName("liquefactionRisk")]
    public int LiquefactionRisk { get; init; }

    [JsonPropertyName("overallRiskScore")]
    public int OverallRiskScore { get; init; }

    [JsonPropertyName("overallRiskLevel")]
    public string? OverallRiskLevel { get; init; }

    [JsonPropertyName("flood")]
    public RiskSubResultPayload? Flood { get; init; }

    [JsonPropertyName("seismic")]
    public RiskSubResultPayload? Seismic { get; init; }

    [JsonPropertyName("expansiveSoil")]
    public RiskSubResultPayload? ExpansiveSoil { get; init; }

    [JsonPropertyName("liquefaction")]
    public RiskSubResultPayload? Liquefaction { get; init; }

    [JsonPropertyName("riskHeatmapTileUrl")]
    public string? RiskHeatmapTileUrl { get; init; }

    [JsonPropertyName("mitigationSuggestions")]
    public List<RiskMitigationSuggestionPayload>? MitigationSuggestions { get; init; }
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
