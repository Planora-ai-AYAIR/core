using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Webhooks.Payloads;

public sealed record RiskSubResultPayload
{
    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("factors")]
    public List<string>? Factors { get; init; }

    [JsonPropertyName("geoJsonUrl")]
    public string? GeoJsonUrl { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("replacementDepth")]
    public double? ReplacementDepth { get; init; }

    [JsonPropertyName("susceptibility")]
    public string? Susceptibility { get; init; }
}
