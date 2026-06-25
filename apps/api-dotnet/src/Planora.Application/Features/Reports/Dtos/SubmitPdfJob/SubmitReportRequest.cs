namespace Planora.Application.Features.Reports.Dtos.SubmitPdfJob;

public sealed record SubmitReportRequest(
    string? Language,
    string? CompanyName,
    string? ProjectName,
    bool IncludeMaps,
    bool IncludeTables,
    bool IncludeRiskMatrix,
    bool IncludeBoreholePlan,
    string? DisclaimerLevel
);
