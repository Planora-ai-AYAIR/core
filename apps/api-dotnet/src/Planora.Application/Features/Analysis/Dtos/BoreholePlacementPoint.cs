using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record BoreholePlacementPoint
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    [JsonPropertyName("priority")]
    public string Priority { get; init; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("estimatedDepth")]
    public double? EstimatedDepth { get; init; }
}
