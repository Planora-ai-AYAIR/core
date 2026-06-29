using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record TopographyResultPayload
{
    [JsonPropertyName("elevation")]
    public ElevationPayload? Elevation { get; init; }

    [JsonPropertyName("slopeDistribution")]
    public List<SlopeCategoryEntry>? SlopeDistribution { get; init; }

    [JsonPropertyName("cutFillAnalysis")]
    public CutFillAnalysisPayload? CutFillAnalysis { get; init; }

    [JsonPropertyName("pondingRisk")]
    public PondingRiskPayload? PondingRisk { get; init; }

    [JsonPropertyName("visualizationAssets")]
    public TopographyAssetsPayload? VisualizationAssets { get; init; }

    [JsonPropertyName("metadata")]
    public TopographyMetadataPayload? Metadata { get; init; }
}

public sealed record ElevationPayload
{
    [JsonPropertyName("minimumMeters")]
    public double MinimumMeters { get; init; }

    [JsonPropertyName("maximumMeters")]
    public double MaximumMeters { get; init; }

    [JsonPropertyName("averageMeters")]
    public double AverageMeters { get; init; }
}

public sealed record CutFillAnalysisPayload
{
    [JsonPropertyName("cutVolumeM3")]
    public double CutVolumeM3 { get; init; }

    [JsonPropertyName("fillVolumeM3")]
    public double FillVolumeM3 { get; init; }

    [JsonPropertyName("netVolumeM3")]
    public double NetVolumeM3 { get; init; }
}

public sealed record PondingRiskPayload
{
    [JsonPropertyName("riskLevel")]
    public string? RiskLevel { get; init; }

    [JsonPropertyName("zonesCount")]
    public int? ZonesCount { get; init; }

    [JsonPropertyName("affectedAreaM2")]
    public double? AffectedAreaM2 { get; init; }
}

public sealed record TopographyAssetsPayload
{
    [JsonPropertyName("elevationTileUrl")]
    public string? ElevationTileUrl { get; init; }

    [JsonPropertyName("slopeTileUrl")]
    public string? SlopeTileUrl { get; init; }

    [JsonPropertyName("contourGeoJsonUrl")]
    public string? ContourGeoJsonUrl { get; init; }

    [JsonPropertyName("pondingGeoJsonUrl")]
    public string? PondingGeoJsonUrl { get; init; }

    [JsonPropertyName("demRasterUrl")]
    public string? DemRasterUrl { get; init; }

    [JsonPropertyName("slopeRasterUrl")]
    public string? SlopeRasterUrl { get; init; }
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
