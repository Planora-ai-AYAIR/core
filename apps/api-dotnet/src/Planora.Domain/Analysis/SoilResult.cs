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


    // Bearing module (derived by Python alongside soil classification). [Not Listed in Soil Brackets in API Contract]
    public double BearingCapacityEstimate { get; private set; }
    public string BearingCapacityCategory { get; private set; } = string.Empty;
    public string? OrganicCarbonUnit { get; private set; }
    public double? BearingConfidence { get; private set; }
    public string? BearingRange { get; private set; }
    public string? BearingTrafficLight { get; private set; }
    public string? RecommendedFoundation { get; private set; }
    public int? MaxFloorsWithoutDeepFoundation { get; private set; }
    public string? FloorCountCategory { get; private set; }
    public double? BearingMinKpa { get; private set; }
    public double? BearingMaxKpa { get; private set; }
    public string? FeatureImportanceJson { get; private set; }
    public string? SoilFactorsJson { get; private set; }
    public string? BearingModelName { get; private set; }
    public string? BearingFramework { get; private set; }
    public double? BearingTrainingR2 { get; private set; }
    public bool? BearingShapEnabled { get; private set; }

    private SoilResult() { }

    public SoilResult(
        Guid analysisJobId,
        double sandPercent,
        double siltPercent,
        double clayPercent,
        double bulkDensity,
        double organicCarbon,
        double ph,
        double bearingCapacityEstimate,
        string bearingCapacityCategory,
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
        double? ndmiMean = null,
        double? bearingConfidence = null,
        string? bearingRange = null,
        string? bearingTrafficLight = null,
        string? recommendedFoundation = null,
        int? maxFloorsWithoutDeepFoundation = null,
        string? floorCountCategory = null,
        double? bearingMinKpa = null,
        double? bearingMaxKpa = null,
        string? featureImportanceJson = null,
        string? soilFactorsJson = null,
        string? bearingModelName = null,
        string? bearingFramework = null,
        double? bearingTrainingR2 = null,
        bool? bearingShapEnabled = null)
    {
        AnalysisJobId = analysisJobId;
        SandPercent = sandPercent;
        SiltPercent = siltPercent;
        ClayPercent = clayPercent;
        BulkDensity = bulkDensity;
        OrganicCarbon = organicCarbon;
        Ph = ph;
        Cec = cec;
        WaterTableDepthMeters = waterTableDepthMeters;
        BearingCapacityEstimate = bearingCapacityEstimate;
        BearingCapacityCategory = bearingCapacityCategory;
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
        BearingConfidence = bearingConfidence;
        BearingRange = bearingRange;
        BearingTrafficLight = bearingTrafficLight;
        RecommendedFoundation = recommendedFoundation;
        MaxFloorsWithoutDeepFoundation = maxFloorsWithoutDeepFoundation;
        FloorCountCategory = floorCountCategory;
        BearingMinKpa = bearingMinKpa;
        BearingMaxKpa = bearingMaxKpa;
        FeatureImportanceJson = featureImportanceJson;
        SoilFactorsJson = soilFactorsJson;
        BearingModelName = bearingModelName;
        BearingFramework = bearingFramework;
        BearingTrainingR2 = bearingTrainingR2;
        BearingShapEnabled = bearingShapEnabled;
    }
}
