using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record SlopeCategoryEntry
{
    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("range")]
    public string Range { get; init; } = string.Empty;

    [JsonPropertyName("percentage")]
    public double Percentage { get; init; }

    [JsonPropertyName("color")]
    public string Color { get; init; } = string.Empty;
}
