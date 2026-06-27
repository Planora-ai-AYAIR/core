namespace Planora.Application.Common.Dtos;

public record ProccessPdfJobAiRequest(
    Guid ParcelId,
    Guid ReportId,
    string? Language = "en",
    bool IncludeMaps = true,
    bool IncludeTables = true,
    bool IncludeRiskMatrix = true,
    string? DisclaimerLevel = "full",
    string? CompanyName = null,
    string? ProjectName = null
);
