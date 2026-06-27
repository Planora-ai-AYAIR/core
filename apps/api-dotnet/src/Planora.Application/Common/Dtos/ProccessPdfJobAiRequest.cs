using System.Text.Json.Serialization;

namespace Planora.Application.Common.Dtos;

public sealed record ProccessPdfJobAiRequest(
    [property: JsonPropertyName("jobId")] string JobId,
    [property: JsonPropertyName("parcelId")] string ParcelId,
    [property: JsonPropertyName("moduleResults")] Dictionary<string, object>? ModuleResults = null,
    [property: JsonPropertyName("reportOptions")] PdfReportOptions? ReportOptions = null);

public sealed record PdfReportOptions(
    [property: JsonPropertyName("companyName")] string? CompanyName = null,
    [property: JsonPropertyName("projectName")] string? ProjectName = null,
    [property: JsonPropertyName("language")] string Language = "en");
