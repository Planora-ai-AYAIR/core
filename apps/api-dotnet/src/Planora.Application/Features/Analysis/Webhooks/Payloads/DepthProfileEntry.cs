using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Webhooks.Payloads;

public sealed record DepthProfileEntry
{
    [JsonPropertyName("depth")]
    public string Depth { get; init; } = string.Empty;

    [JsonPropertyName("sand")]
    public double Sand { get; init; }

    [JsonPropertyName("clay")]
    public double Clay { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}
