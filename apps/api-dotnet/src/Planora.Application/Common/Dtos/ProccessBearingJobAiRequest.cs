using System.Text.Json.Serialization;

namespace Planora.Application.Common.Dtos;

public sealed record ProccessBearingJobAiRequest(
    [property: JsonPropertyName("jobId")] string JobId,
    [property: JsonPropertyName("parcelId")] string ParcelId,
    [property: JsonPropertyName("geoJson")] AiGeoJsonPolygon GeoJson,
    [property: JsonPropertyName("bbox")] AiBoundingBox? Bbox = null,
    [property: JsonPropertyName("soilData")] BearingSoilData? SoilData = null);

public sealed record BearingSoilData(
    [property: JsonPropertyName("clayContent")] double? ClayContent = null,
    [property: JsonPropertyName("sandContent")] double? SandContent = null,
    [property: JsonPropertyName("siltContent")] double? SiltContent = null,
    [property: JsonPropertyName("bulkDensity")] double? BulkDensity = null,
    [property: JsonPropertyName("waterTableDepth")] double? WaterTableDepth = null);
