using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Webhooks.Payloads;

public sealed record AnalysisFailurePayload
{
    [JsonPropertyName("pythonJobId")]
    public string PythonJobId { get; init; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; init; }
}