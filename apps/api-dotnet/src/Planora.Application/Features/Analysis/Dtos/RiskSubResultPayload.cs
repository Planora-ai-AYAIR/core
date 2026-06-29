using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record RiskSubResultPayload
{
    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("weight")]
    public double? Weight { get; init; }

    [JsonPropertyName("factors")]
    public List<string>? Factors { get; init; }

    [JsonPropertyName("zonesGeoJsonUrl")]
    public string? ZonesGeoJsonUrl { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("zone")]
    public string? Zone { get; init; }

    [JsonPropertyName("replacementDepthMeters")]
    public double? ReplacementDepthMeters { get; init; }

    [JsonPropertyName("susceptibility")]
    public string? Susceptibility { get; init; }

    [JsonPropertyName("methodology")]
    public string? Methodology { get; init; }
}
