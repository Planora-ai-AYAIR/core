using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record SoilResultPayload
{
    [JsonPropertyName("pythonJobId")]
    public string PythonJobId { get; init; } = string.Empty;

    [JsonPropertyName("sandPercent")]
    public double SandPercent { get; init; }

    [JsonPropertyName("siltPercent")]
    public double SiltPercent { get; init; }

    [JsonPropertyName("clayPercent")]
    public double ClayPercent { get; init; }

    [JsonPropertyName("bulkDensity")]
    public double BulkDensity { get; init; }

    [JsonPropertyName("organicCarbon")]
    public double OrganicCarbon { get; init; }

    [JsonPropertyName("ph")]
    public double Ph { get; init; }

    [JsonPropertyName("cec")]
    public double? Cec { get; init; }

    [JsonPropertyName("waterTableDepthMeters")]
    public double? WaterTableDepthMeters { get; init; }

    [JsonPropertyName("bearingCapacityEstimate")]
    public double BearingCapacityEstimate { get; init; }

    [JsonPropertyName("bearingCapacityCategory")]
    public string BearingCapacityCategory { get; init; } = string.Empty;

    [JsonPropertyName("compositionUnit")]
    public string? CompositionUnit { get; init; }

    [JsonPropertyName("bulkDensityUnit")]
    public string? BulkDensityUnit { get; init; }

    [JsonPropertyName("organicCarbonUnit")]
    public string? OrganicCarbonUnit { get; init; }

    [JsonPropertyName("primaryType")]
    public string? PrimaryType { get; init; }

    [JsonPropertyName("usdaClass")]
    public string? UsdaClass { get; init; }

    [JsonPropertyName("aiConfidence")]
    public double? AiConfidence { get; init; }

    [JsonPropertyName("depthProfiles")]
    public List<DepthProfileEntry>? DepthProfiles { get; init; }

    [JsonPropertyName("heatmapTileUrl")]
    public string? HeatmapTileUrl { get; init; }

    [JsonPropertyName("soilTypeGeoJsonUrl")]
    public string? SoilTypeGeoJsonUrl { get; init; }

    [JsonPropertyName("depthProfileImageUrl")]
    public string? DepthProfileImageUrl { get; init; }

    [JsonPropertyName("dataSources")]
    public List<string>? DataSources { get; init; }

    [JsonPropertyName("spectralIndices")]
    public SoilSpectralIndicesPayload? SpectralIndices { get; init; }

    [JsonPropertyName("bearing")]
    public BearingPayload? Bearing { get; init; }
}

public sealed record SoilSpectralIndicesPayload
{
    [JsonPropertyName("ndviMean")]
    public double NdviMean { get; init; }

    [JsonPropertyName("bsiMean")]
    public double BsiMean { get; init; }

    [JsonPropertyName("ndmiMean")]
    public double NdmiMean { get; init; }
}

public sealed record BearingPayload
{
    [JsonPropertyName("confidence")]
    public double? Confidence { get; init; }

    [JsonPropertyName("range")]
    public string? Range { get; init; }

    [JsonPropertyName("trafficLight")]
    public string? TrafficLight { get; init; }

    [JsonPropertyName("recommendedFoundation")]
    public string? RecommendedFoundation { get; init; }

    [JsonPropertyName("maxFloorsWithoutDeepFoundation")]
    public int? MaxFloorsWithoutDeepFoundation { get; init; }

    [JsonPropertyName("floorCountCategory")]
    public string? FloorCountCategory { get; init; }

    [JsonPropertyName("uncertaintyRange")]
    public BearingUncertaintyRangePayload? UncertaintyRange { get; init; }

    [JsonPropertyName("featureImportance")]
    public List<FeatureImportanceEntry>? FeatureImportance { get; init; }

    [JsonPropertyName("soilFactors")]
    public BearingSoilFactorsPayload? SoilFactors { get; init; }

    [JsonPropertyName("modelMetadata")]
    public BearingModelMetadataPayload? ModelMetadata { get; init; }
}

public sealed record BearingUncertaintyRangePayload
{
    [JsonPropertyName("minimumKpa")]
    public double MinimumKpa { get; init; }

    [JsonPropertyName("maximumKpa")]
    public double MaximumKpa { get; init; }
}

public sealed record FeatureImportanceEntry
{
    [JsonPropertyName("feature")]
    public string Feature { get; init; } = string.Empty;

    [JsonPropertyName("weight")]
    public double Weight { get; init; }
}

public sealed record BearingSoilFactorsPayload
{
    [JsonPropertyName("clayContent")]
    public double ClayContent { get; init; }

    [JsonPropertyName("sandContent")]
    public double SandContent { get; init; }

    [JsonPropertyName("moistureIndex")]
    public double MoistureIndex { get; init; }

    [JsonPropertyName("depthToWaterTableMeters")]
    public double DepthToWaterTableMeters { get; init; }

    [JsonPropertyName("terrainSlopePercent")]
    public double TerrainSlopePercent { get; init; }
}

public sealed record BearingModelMetadataPayload
{
    [JsonPropertyName("modelName")]
    public string ModelName { get; init; } = string.Empty;

    [JsonPropertyName("framework")]
    public string Framework { get; init; } = string.Empty;

    [JsonPropertyName("trainingR2")]
    public double TrainingR2 { get; init; }

    [JsonPropertyName("shapEnabled")]
    public bool ShapEnabled { get; init; }
}
