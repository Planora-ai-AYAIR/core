using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record SoilResultPayload
{
    [JsonPropertyName("classification")]
    public SoilClassificationPayload? Classification { get; init; }

    [JsonPropertyName("surfaceComposition")]
    public SoilSurfaceCompositionPayload? SurfaceComposition { get; init; }

    [JsonPropertyName("properties")]
    public SoilPropertiesPayload? Properties { get; init; }

    [JsonPropertyName("depthLayers")]
    public List<SoilDepthLayerPayload>? DepthLayers { get; init; }

    [JsonPropertyName("visualizationAssets")]
    public SoilAssetsPayload? VisualizationAssets { get; init; }

    [JsonPropertyName("dataSources")]
    public List<string>? DataSources { get; init; }

    [JsonPropertyName("spectralIndices")]
    public SoilSpectralIndicesPayload? SpectralIndices { get; init; }
}

public sealed record SoilClassificationPayload
{
    [JsonPropertyName("primaryType")]
    public string? PrimaryType { get; init; }

    [JsonPropertyName("usdaClass")]
    public string? UsdaClass { get; init; }

    [JsonPropertyName("aiConfidence")]
    public double? AiConfidence { get; init; }
}

public sealed record SoilSurfaceCompositionPayload
{
    [JsonPropertyName("sandPercentage")]
    public double SandPercentage { get; init; }

    [JsonPropertyName("siltPercentage")]
    public double SiltPercentage { get; init; }

    [JsonPropertyName("clayPercentage")]
    public double ClayPercentage { get; init; }

    [JsonPropertyName("unit")]
    public string? Unit { get; init; }
}

public sealed record SoilPropertiesPayload
{
    [JsonPropertyName("bulkDensity")]
    public double BulkDensity { get; init; }

    [JsonPropertyName("bulkDensityUnit")]
    public string? BulkDensityUnit { get; init; }

    [JsonPropertyName("organicCarbonPercentage")]
    public double OrganicCarbonPercentage { get; init; }

    [JsonPropertyName("ph")]
    public double Ph { get; init; }

    [JsonPropertyName("cec")]
    public double? Cec { get; init; }

    [JsonPropertyName("waterTableDepthMeters")]
    public double? WaterTableDepthMeters { get; init; }
}

public sealed record SoilDepthLayerPayload
{
    [JsonPropertyName("depth")]
    public string Depth { get; init; } = string.Empty;

    [JsonPropertyName("sand")]
    public double Sand { get; init; }

    [JsonPropertyName("silt")]
    public double Silt { get; init; }

    [JsonPropertyName("clay")]
    public double Clay { get; init; }

    [JsonPropertyName("soilType")]
    public string? SoilType { get; init; }

    [JsonPropertyName("bulkDensity")]
    public double? BulkDensity { get; init; }
}

public sealed record SoilAssetsPayload
{
    [JsonPropertyName("soilHeatmapTileUrl")]
    public string? SoilHeatmapTileUrl { get; init; }

    [JsonPropertyName("soilTypeGeoJsonUrl")]
    public string? SoilTypeGeoJsonUrl { get; init; }

    [JsonPropertyName("depthProfileImageUrl")]
    public string? DepthProfileImageUrl { get; init; }
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

// ── Shared Bearing sub-records (also consumed by BearingResultPayload) ──────

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
