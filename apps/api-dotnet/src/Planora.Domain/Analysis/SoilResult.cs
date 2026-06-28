using Planora.Domain.Shared.Abstractions;

namespace Planora.Domain.Analysis;

public sealed class SoilResult : AuditableEntity
{
    public Guid AnalysisJobId { get; private set; }

    // Classification
    public string? PrimaryType { get; private set; }
    public string? UsdaClass { get; private set; }
    public double? AiConfidence { get; private set; }

    // Surface Composition
    public double SandPercent { get; private set; }
    public double SiltPercent { get; private set; }
    public double ClayPercent { get; private set; }
    public string? CompositionUnit { get; private set; }

    // Properties
    public double BulkDensity { get; private set; }
    public string? BulkDensityUnit { get; private set; }
    public double OrganicCarbon { get; private set; }
    public string? OrganicCarbonUnit { get; private set; }
    public double Ph { get; private set; }
    public double? Cec { get; private set; }
    public double? WaterTableDepthMeters { get; private set; }

    // Depth Layers
    public string? MultiDepthProfileJson { get; private set; }

    // Soil Visualization Assets.
    public string? HeatmapTileUrl { get; private set; }
    public string? SoilTypeGeoJsonUrl { get; private set; }
    public string? DepthProfileImageUrl { get; private set; }

    // Data Sources
    public string? DataSourcesJson { get; private set; }

    // Spectral Indices
    public double? NdviMean { get; private set; }
    public double? BsiMean { get; private set; }
    public double? NdmiMean { get; private set; }

    private SoilResult() { }

    public SoilResult(
        Guid analysisJobId,
        double sandPercent,
        double siltPercent,
        double clayPercent,
        double bulkDensity,
        double organicCarbon,
        double ph,
        string? compositionUnit = null,
        string? bulkDensityUnit = null,
        string? organicCarbonUnit = null,
        string? primaryType = null,
        string? usdaClass = null,
        double? aiConfidence = null,
        string? multiDepthProfileJson = null,
        string? heatmapTileUrl = null,
        double? cec = null,
        double? waterTableDepthMeters = null,
        string? soilTypeGeoJsonUrl = null,
        string? depthProfileImageUrl = null,
        string? dataSourcesJson = null,
        double? ndviMean = null,
        double? bsiMean = null,
        double? ndmiMean = null)
    {
        Id = Guid.NewGuid();
        AnalysisJobId = analysisJobId;
        SandPercent = sandPercent;
        SiltPercent = siltPercent;
        ClayPercent = clayPercent;
        BulkDensity = bulkDensity;
        OrganicCarbon = organicCarbon;
        Ph = ph;
        Cec = cec;
        WaterTableDepthMeters = waterTableDepthMeters;
        CompositionUnit = compositionUnit;
        BulkDensityUnit = bulkDensityUnit;
        OrganicCarbonUnit = organicCarbonUnit;
        PrimaryType = primaryType;
        UsdaClass = usdaClass;
        AiConfidence = aiConfidence;
        MultiDepthProfileJson = multiDepthProfileJson;
        HeatmapTileUrl = heatmapTileUrl;
        SoilTypeGeoJsonUrl = soilTypeGeoJsonUrl;
        DepthProfileImageUrl = depthProfileImageUrl;
        DataSourcesJson = dataSourcesJson;
        NdviMean = ndviMean;
        BsiMean = bsiMean;
        NdmiMean = ndmiMean;
    }
}
