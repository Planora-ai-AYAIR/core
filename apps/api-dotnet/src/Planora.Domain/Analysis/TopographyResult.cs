using Planora.Domain.Shared.Abstractions;

namespace Planora.Domain.Analysis;

public sealed class TopographyResult : AuditableEntity
{
    public Guid AnalysisJobId { get; private set; }

    // Elevation
    public double ElevationMin { get; private set; }
    public double ElevationMax { get; private set; }
    public double ElevationMean { get; private set; }

    // SlopeDistributionJson
    public string SlopeDistributionJson { get; private set; } = string.Empty;

    // Cut Fill Analysis
    public double CutVolume { get; private set; }
    public double FillVolume { get; private set; }
    public double NetVolume { get; private set; }

    // Topography Visualization Assets
    public string? ElevationTileUrl { get; private set; }
    public string? SlopeTileUrl { get; private set; }
    public string? ContourGeoJsonUrl { get; private set; }
    public string? PondingGeoJsonUrl { get; private set; }
    public string? DemRasterUrl { get; private set; }
    public string? SlopeRasterUrl { get; private set; }

    // Metadata
    public string? CopernicusDemVersion { get; private set; }
    public int? PixelResolutionMeters { get; private set; }
    public string? Crs { get; private set; }
    public int? ProcessingTimeSeconds { get; private set; }

    // Additions
    public double ContourInterval { get; private set; }
    public int? PondingZonesCount { get; private set; }
    public double? PondingTotalArea { get; private set; }

    private TopographyResult() { }

    public TopographyResult(
        Guid analysisJobId,
        double elevationMin,
        double elevationMax,
        double elevationMean,
        string slopeDistributionJson,
        double cutVolume,
        double fillVolume,
        double netVolume,
        double contourInterval,
        string? contourGeoJsonUrl = null,
        string? pondingGeoJsonUrl = null,
        int? pondingZonesCount = null,
        double? pondingTotalArea = null,
        string? elevationTileUrl = null,
        string? slopeTileUrl = null,
        string? demRasterUrl = null,
        string? slopeRasterUrl = null,
        string? copernicusDemVersion = null,
        int? pixelResolutionMeters = null,
        string? crs = null,
        int? processingTimeSeconds = null)
    {
        Id = Guid.NewGuid();
        AnalysisJobId = analysisJobId;
        ElevationMin = elevationMin;
        ElevationMax = elevationMax;
        ElevationMean = elevationMean;
        SlopeDistributionJson = slopeDistributionJson;
        CutVolume = cutVolume;
        FillVolume = fillVolume;
        NetVolume = netVolume;
        ContourInterval = contourInterval;
        ContourGeoJsonUrl = contourGeoJsonUrl;
        PondingGeoJsonUrl = pondingGeoJsonUrl;
        PondingZonesCount = pondingZonesCount;
        PondingTotalArea = pondingTotalArea;
        ElevationTileUrl = elevationTileUrl;
        SlopeTileUrl = slopeTileUrl;
        DemRasterUrl = demRasterUrl;
        SlopeRasterUrl = slopeRasterUrl;
        CopernicusDemVersion = copernicusDemVersion;
        PixelResolutionMeters = pixelResolutionMeters;
        Crs = crs;
        ProcessingTimeSeconds = processingTimeSeconds;
    }
}
