using System.Text.Json.Serialization;

namespace Planora.Application.Common.Dtos;

public sealed record AiGeoJsonPolygon(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("coordinates")] List<List<List<double>>> Coordinates);

public sealed record AiBoundingBox(
    [property: JsonPropertyName("minX")] double MinX,
    [property: JsonPropertyName("minY")] double MinY,
    [property: JsonPropertyName("maxX")] double MaxX,
    [property: JsonPropertyName("maxY")] double MaxY);
