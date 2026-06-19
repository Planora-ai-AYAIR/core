using System.Text.Json.Serialization;

namespace Planora.Application.Features.Analysis.Webhooks.Payloads;

public sealed record PdfResultPayload
{
    [JsonPropertyName("pythonJobId")]
    public string PythonJobId { get; init; } = string.Empty;

    [JsonPropertyName("pdfS3Url")]
    public string PdfS3Url { get; init; } = string.Empty;

    [JsonPropertyName("pageCount")]
    public int? PageCount { get; init; }

    [JsonPropertyName("sizeBytes")]
    public long? SizeBytes { get; init; }
}
