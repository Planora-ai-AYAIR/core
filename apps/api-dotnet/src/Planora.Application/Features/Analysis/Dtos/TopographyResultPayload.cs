using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record TopographyResultPayload
{
    [JsonPropertyName("pythonJobId")]
    public string PythonJobId { get; init; } = string.Empty;

    [JsonPropertyName("elevationMin")]
    public double ElevationMin { get; init; }

    [JsonPropertyName("elevationMax")]
    public double ElevationMax { get; init; }

    [JsonPropertyName("elevationMean")]
    public double ElevationMean { get; init; }

    [JsonPropertyName("slopeDistribution")]
    public List<SlopeCategoryEntry>? SlopeDistribution { get; init; }

    [JsonPropertyName("cutVolume")]
    public double CutVolume { get; init; }

    [JsonPropertyName("fillVolume")]
    public double FillVolume { get; init; }

    [JsonPropertyName("netVolume")]
    public double NetVolume { get; init; }

    [JsonPropertyName("contourInterval")]
    public double ContourInterval { get; init; }

    [JsonPropertyName("contourGeoJsonUrl")]
    public string? ContourGeoJsonUrl { get; init; }

    [JsonPropertyName("pondingGeoJsonUrl")]
    public string? PondingGeoJsonUrl { get; init; }

    [JsonPropertyName("pondingZonesCount")]
    public int? PondingZonesCount { get; init; }

    [JsonPropertyName("pondingTotalArea")]
    public double? PondingTotalArea { get; init; }

    [JsonPropertyName("elevationTileUrl")]
    public string? ElevationTileUrl { get; init; }

    [JsonPropertyName("slopeTileUrl")]
    public string? SlopeTileUrl { get; init; }

    [JsonPropertyName("demRasterUrl")]
    public string? DemRasterUrl { get; init; }

    [JsonPropertyName("slopeRasterUrl")]
    public string? SlopeRasterUrl { get; init; }

    [JsonPropertyName("metadata")]
    public TopographyMetadataPayload? Metadata { get; init; }
}

public sealed record TopographyMetadataPayload
{
    [JsonPropertyName("copernicusDemVersion")]
    public string? CopernicusDemVersion { get; init; }

    [JsonPropertyName("pixelResolutionMeters")]
    public int? PixelResolutionMeters { get; init; }

    [JsonPropertyName("crs")]
    public string? Crs { get; init; }

    [JsonPropertyName("processingTimeSeconds")]
    public int? ProcessingTimeSeconds { get; init; }
}
