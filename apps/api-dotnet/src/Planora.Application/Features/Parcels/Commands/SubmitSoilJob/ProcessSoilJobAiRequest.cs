using System.Text.Json.Serialization;

namespace Planora.Application.Common.Dtos;

public sealed record ProccessSoilJobAiRequest(
    [property: JsonPropertyName("jobId")] string JobId,
    [property: JsonPropertyName("parcelId")] string ParcelId,
    [property: JsonPropertyName("geoJson")] AiGeoJsonPolygon GeoJson,
    [property: JsonPropertyName("bbox")] AiBoundingBox? Bbox = null,
    [property: JsonPropertyName("depths")] List<string>? Depths = null);
