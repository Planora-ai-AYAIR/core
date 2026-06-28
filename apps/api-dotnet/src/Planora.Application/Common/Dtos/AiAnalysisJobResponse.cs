using System.Text.Json.Serialization;

namespace Planora.Application.Common.Dtos;

public sealed record AiAnalysisJobResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] AiAnalysisJobData Data);

public sealed record AiAnalysisJobData(
    [property: JsonPropertyName("pythonJobId")] string JobId,
    [property: JsonPropertyName("backendJobId")] string BackendJobId,
    [property: JsonPropertyName("parcelId")] string ParcelId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("acceptedAt")] DateTime AcceptedAt,
    [property: JsonPropertyName("estimatedDuration")] string EstimatedDuration);
