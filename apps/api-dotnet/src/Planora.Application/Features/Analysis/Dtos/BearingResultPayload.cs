using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record BearingResultPayload(
    [property: JsonPropertyName("bearingCapacityKpa")] double BearingCapacityKpa,
    [property: JsonPropertyName("confidence")] double? Confidence = null,
    [property: JsonPropertyName("classification")] string? Classification = null,
    [property: JsonPropertyName("range")] string? Range = null,
    [property: JsonPropertyName("trafficLight")] string? TrafficLight = null,
    [property: JsonPropertyName("recommendedFoundation")] string? RecommendedFoundation = null,
    [property: JsonPropertyName("maxFloorsWithoutDeepFoundation")] int? MaxFloorsWithoutDeepFoundation = null,
    [property: JsonPropertyName("floorCountCategory")] string? FloorCountCategory = null,
    [property: JsonPropertyName("uncertaintyRange")] BearingUncertaintyRangePayload? UncertaintyRange = null,
    [property: JsonPropertyName("featureImportance")] List<FeatureImportanceEntry>? FeatureImportance = null,
    [property: JsonPropertyName("soilFactors")] BearingSoilFactorsPayload? SoilFactors = null,
    [property: JsonPropertyName("disclaimer")] string? Disclaimer = null,
    [property: JsonPropertyName("modelMetadata")] BearingModelMetadataPayload? ModelMetadata = null);
