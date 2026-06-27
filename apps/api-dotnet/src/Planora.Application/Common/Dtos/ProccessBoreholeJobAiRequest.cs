using System.Text.Json.Serialization;

namespace Planora.Application.Common.Dtos;

public sealed record ProccessBoreholeJobAiRequest(
    [property: JsonPropertyName("jobId")] string JobId,
    [property: JsonPropertyName("parcelId")] string ParcelId,
    [property: JsonPropertyName("geoJson")] AiGeoJsonPolygon GeoJson,
    [property: JsonPropertyName("bbox")] AiBoundingBox? Bbox = null,
    [property: JsonPropertyName("parameters")] BoreholeParameters? Parameters = null);

public sealed record BoreholeParameters(
    [property: JsonPropertyName("maxSpacing")] int MaxSpacing = 30,
    [property: JsonPropertyName("minBoreholes")] int MinBoreholes = 12,
    [property: JsonPropertyName("targetDepth")] int TargetDepth = 20);
