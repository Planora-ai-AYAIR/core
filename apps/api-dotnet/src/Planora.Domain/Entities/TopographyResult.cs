using Planora.Domain.Shared.Abstractions;

namespace Planora.Domain.Entities;

public sealed class TopographyResult : AuditableEntity
{
    public Guid AnalysisJobId { get; private set; }
    public double ElevationMin { get; private set; }
    public double ElevationMax { get; private set; }
    public double ElevationMean { get; private set; }
    public string SlopeDistributionJson { get; private set; } = string.Empty;
    public double CutVolume { get; private set; }
    public double FillVolume { get; private set; }
    public double NetVolume { get; private set; }
    public double ContourInterval { get; private set; }
    public string? ContourGeoJsonUrl { get; private set; }
    public string? PondingGeoJsonUrl { get; private set; }
    public int? PondingZonesCount { get; private set; }
    public double? PondingTotalArea { get; private set; }
    public string? ElevationTileUrl { get; private set; }
    public string? SlopeTileUrl { get; private set; }

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
        string? slopeTileUrl = null)
    {
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
    }
}
