using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Dtos;

public sealed record AggregatedAnalysisResultPayload(
    [property: JsonPropertyName("pythonJobId")] string PythonJobId = "",
    [property: JsonPropertyName("backendJobId")] string? BackendJobId = null,
    [property: JsonPropertyName("parcelId")] string? ParcelId = null,
    [property: JsonPropertyName("status")] string? Status = null,
    [property: JsonPropertyName("startedAt")] DateTime? StartedAt = null,
    [property: JsonPropertyName("completedAt")] DateTime? CompletedAt = null,
    [property: JsonPropertyName("processingTimeSeconds")] int? ProcessingTimeSeconds = null,
    [property: JsonPropertyName("result")] AggregatedAnalysisResult? Result = null);

public sealed record AggregatedAnalysisResult(
    [property: JsonPropertyName("topography")] TopographyResultPayload? Topography = null,
    [property: JsonPropertyName("soil")] SoilResultPayload? Soil = null,
    [property: JsonPropertyName("bearing")] BearingResultPayload? Bearing = null,
    [property: JsonPropertyName("risk")] RiskResultPayload? Risk = null,
    [property: JsonPropertyName("borehole")] BoreholeResultPayload? Borehole = null);
