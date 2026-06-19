using Planora.Domain.Shared.Abstractions;

namespace Planora.Domain.Entities;

public sealed class SoilResult : AuditableEntity
{
    public Guid AnalysisJobId { get; private set; }
    public double SandPercent { get; private set; }
    public double SiltPercent { get; private set; }
    public double ClayPercent { get; private set; }
    public double BulkDensity { get; private set; }
    public double OrganicCarbon { get; private set; }
    public double Ph { get; private set; }
    public double BearingCapacityEstimate { get; private set; }
    public string BearingCapacityCategory { get; private set; } = string.Empty;
    public string? CompositionUnit { get; private set; }
    public string? BulkDensityUnit { get; private set; }
    public string? OrganicCarbonUnit { get; private set; }
    public string? PrimaryType { get; private set; }
    public string? UsdaClass { get; private set; }
    public double? AiConfidence { get; private set; }
    public string? MultiDepthProfileJson { get; private set; }
    public string? HeatmapTileUrl { get; private set; }

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
        string? heatmapTileUrl = null)
    {
        AnalysisJobId = analysisJobId;
        SandPercent = sandPercent;
        SiltPercent = siltPercent;
        ClayPercent = clayPercent;
        BulkDensity = bulkDensity;
        OrganicCarbon = organicCarbon;
        Ph = ph;
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
    }
}
