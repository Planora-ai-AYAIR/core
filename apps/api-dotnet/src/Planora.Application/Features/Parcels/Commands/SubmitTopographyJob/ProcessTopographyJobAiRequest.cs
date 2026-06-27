using System.Text.Json.Serialization;

namespace Planora.Application.Common.Dtos;

public sealed record ProccessTopographyJobAiRequest(
    [property: JsonPropertyName("jobId")] string JobId,
    [property: JsonPropertyName("parcelId")] string ParcelId,
    [property: JsonPropertyName("geoJson")] AiGeoJsonPolygon GeoJson,
    [property: JsonPropertyName("bbox")] AiBoundingBox? Bbox = null,
    [property: JsonPropertyName("options")] TopographyOptions? Options = null);

public sealed record TopographyOptions(
    [property: JsonPropertyName("contourInterval")] double ContourInterval = 0.5,
    [property: JsonPropertyName("slopeCategories")] List<int>? SlopeCategories = null,
    [property: JsonPropertyName("generateCutFill")] bool GenerateCutFill = true,
    [property: JsonPropertyName("referencePlane")] string ReferencePlane = "auto");
