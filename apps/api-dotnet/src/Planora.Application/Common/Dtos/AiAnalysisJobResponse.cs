using System.Text.Json.Serialization;

namespace Planora.Application.Common.Dtos;

public sealed record AiAnalysisJobResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] AiAnalysisJobData Data);

public sealed record AiAnalysisJobData(
    [property: JsonPropertyName("jobId")] string JobId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("submittedAtUtc")] DateTime SubmittedAtUtc);
