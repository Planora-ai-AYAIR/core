using System.Text.Json.Serialization;

namespace Planora.Application.Common.Dtos;

public sealed record AiResponseEnvelope<T>(
    [property: JsonPropertyName("statusCode")] int StatusCode,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("errors")] List<AiErrorDetail>? Errors,
    [property: JsonPropertyName("data")] T? Data);

public sealed record AiErrorDetail(
    [property: JsonPropertyName("field")] string? Field,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("message")] string? Message);

public sealed record AiJobAccepted(
    [property: JsonPropertyName("pythonJobId")] string PythonJobId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("acceptedAt")] DateTimeOffset AcceptedAt);
