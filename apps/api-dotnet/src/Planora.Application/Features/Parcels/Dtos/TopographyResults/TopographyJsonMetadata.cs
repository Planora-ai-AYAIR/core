using System.Text.Json.Serialization;

namespace Planora.Application.Features.Parcels.Dtos.TopographyResults;

public sealed class TopographyJsonMetadata
{
    [JsonPropertyName("elevation_min")]
    public double ElevationMin { get; set; }

    [JsonPropertyName("elevation_max")]
    public double ElevationMax { get; set; }

    [JsonPropertyName("elevation_mean")]
    public double ElevationMean { get; set; }

    [JsonPropertyName("slope_distribution")]
    public List<SlopeDistributionItem> SlopeDistribution { get; set; } = [];

    [JsonPropertyName("cut_volume")]
    public double CutVolume { get; set; }

    [JsonPropertyName("fill_volume")]
    public double FillVolume { get; set; }

    [JsonPropertyName("net_volume")]
    public double NetVolume { get; set; }

    [JsonPropertyName("contour_s3_key")]
    public string? ContourS3Key { get; set; }

    [JsonPropertyName("contour_interval")]
    public double ContourInterval { get; set; } = 0.5;

    [JsonPropertyName("ponding_zones_count")]
    public int PondingZonesCount { get; set; }

    [JsonPropertyName("ponding_total_area")]
    public double PondingTotalArea { get; set; }

    [JsonPropertyName("ponding_s3_key")]
    public string? PondingS3Key { get; set; }

    [JsonPropertyName("elevation_raster_s3_key")]
    public string? ElevationRasterS3Key { get; set; }

    [JsonPropertyName("slope_raster_s3_key")]
    public string? SlopeRasterS3Key { get; set; }

    [JsonPropertyName("generated_at")]
    public DateTime GeneratedAt { get; set; }
}

public sealed class SlopeDistributionItem
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("range")]
    public string Range { get; set; } = string.Empty;

    [JsonPropertyName("percentage")]
    public double Percentage { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
}
