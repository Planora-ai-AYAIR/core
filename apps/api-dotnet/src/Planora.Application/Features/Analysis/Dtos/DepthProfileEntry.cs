using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record DepthProfileEntry
{
    [JsonPropertyName("depth")]
    public string Depth { get; init; } = string.Empty;

    [JsonPropertyName("sand")]
    public double Sand { get; init; }

    [JsonPropertyName("silt")]
    public double Silt { get; init; }

    [JsonPropertyName("clay")]
    public double Clay { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("bulkDensity")]
    public double? BulkDensity { get; init; }
}
