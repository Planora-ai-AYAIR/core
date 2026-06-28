using Planora.Domain.Shared.Abstractions;

namespace Planora.Domain.Analysis;

public sealed class BearingResult : AuditableEntity
{
    public Guid AnalysisJobId { get; private set; }

    public double BearingCapacityKpa { get; private set; }
    public string? Classification { get; private set; }
    public double? Confidence { get; private set; }
    public string? Range { get; private set; }
    public string? TrafficLight { get; private set; }
    public string? RecommendedFoundation { get; private set; }
    public int? MaxFloorsWithoutDeepFoundation { get; private set; }
    public string? FloorCountCategory { get; private set; }
    public double? MinKpa { get; private set; }
    public double? MaxKpa { get; private set; }
    public string? FeatureImportanceJson { get; private set; }
    public string? SoilFactorsJson { get; private set; }
    public string? ModelName { get; private set; }
    public string? Framework { get; private set; }
    public double? TrainingR2 { get; private set; }
    public bool? ShapEnabled { get; private set; }

    private BearingResult() { }

    public BearingResult(
        Guid analysisJobId,
        double bearingCapacityKpa,
        string? classification = null,
        double? confidence = null,
        string? range = null,
        string? trafficLight = null,
        string? recommendedFoundation = null,
        int? maxFloorsWithoutDeepFoundation = null,
        string? floorCountCategory = null,
        double? minKpa = null,
        double? maxKpa = null,
        string? featureImportanceJson = null,
        string? soilFactorsJson = null,
        string? modelName = null,
        string? framework = null,
        double? trainingR2 = null,
        bool? shapEnabled = null)
    {
        Id = Guid.NewGuid();
        AnalysisJobId = analysisJobId;
        BearingCapacityKpa = bearingCapacityKpa;
        Classification = classification;
        Confidence = confidence;
        Range = range;
        TrafficLight = trafficLight;
        RecommendedFoundation = recommendedFoundation;
        MaxFloorsWithoutDeepFoundation = maxFloorsWithoutDeepFoundation;
        FloorCountCategory = floorCountCategory;
        MinKpa = minKpa;
        MaxKpa = maxKpa;
        FeatureImportanceJson = featureImportanceJson;
        SoilFactorsJson = soilFactorsJson;
        ModelName = modelName;
        Framework = framework;
        TrainingR2 = trainingR2;
        ShapEnabled = shapEnabled;
    }

    public void Update(
        double bearingCapacityKpa,
        string? classification = null,
        double? confidence = null,
        string? range = null,
        string? trafficLight = null,
        string? recommendedFoundation = null,
        int? maxFloorsWithoutDeepFoundation = null,
        string? floorCountCategory = null,
        double? minKpa = null,
        double? maxKpa = null,
        string? featureImportanceJson = null,
        string? soilFactorsJson = null,
        string? modelName = null,
        string? framework = null,
        double? trainingR2 = null,
        bool? shapEnabled = null)
    {
        BearingCapacityKpa = bearingCapacityKpa;
        Classification = classification;
        Confidence = confidence;
        Range = range;
        TrafficLight = trafficLight;
        RecommendedFoundation = recommendedFoundation;
        MaxFloorsWithoutDeepFoundation = maxFloorsWithoutDeepFoundation;
        FloorCountCategory = floorCountCategory;
        MinKpa = minKpa;
        MaxKpa = maxKpa;
        FeatureImportanceJson = featureImportanceJson;
        SoilFactorsJson = soilFactorsJson;
        ModelName = modelName;
        Framework = framework;
        TrainingR2 = trainingR2;
        ShapEnabled = shapEnabled;
    }
}
