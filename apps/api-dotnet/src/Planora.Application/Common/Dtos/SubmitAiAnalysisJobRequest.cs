using System.Text.Json.Serialization;

namespace Planora.Application.Common.Dtos;

public sealed record SubmitAiAnalysisJobRequest(
    [property: JsonPropertyName("parcelId")] string ParcelId,
    [property: JsonPropertyName("parcel")] ParcelInfo Parcel,
    [property: JsonPropertyName("boundingBox")] BoundingBoxInfo BoundingBox,
    [property: JsonPropertyName("geometry")] GeometryInfo Geometry,
    [property: JsonPropertyName("analysisOptions")] AnalysisOptionsInfo AnalysisOptions);

public sealed record ParcelInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("areaM2")] decimal AreaM2);

public sealed record BoundingBoxInfo(
    [property: JsonPropertyName("minLatitude")] double MinLatitude,
    [property: JsonPropertyName("minLongitude")] double MinLongitude,
    [property: JsonPropertyName("maxLatitude")] double MaxLatitude,
    [property: JsonPropertyName("maxLongitude")] double MaxLongitude);

public sealed record GeometryInfo(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("coordinates")] List<List<List<double>>> Coordinates);

public sealed record AnalysisOptionsInfo(
    [property: JsonPropertyName("includeTopography")] bool IncludeTopography,
    [property: JsonPropertyName("includeSoil")] bool IncludeSoil,
    [property: JsonPropertyName("includeBearing")] bool IncludeBearing,
    [property: JsonPropertyName("includeRisk")] bool IncludeRisk,
    [property: JsonPropertyName("includeBorehole")] bool IncludeBorehole);
