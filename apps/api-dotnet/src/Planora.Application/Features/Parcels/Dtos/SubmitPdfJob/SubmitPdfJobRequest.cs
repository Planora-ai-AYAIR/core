namespace Planora.Application.Features.Parcels.Dtos.SubmitPdfJob;

public sealed record SubmitPdfJobRequest(
    Guid ParcelId,
    Guid ReportId,
    string? Language = "en",
    bool IncludeMaps = true,
    bool IncludeTables = true,
    bool IncludeRiskMatrix = true,
    string? DisclaimerLevel = "full",
    string? CompanyName = null,
    string? ProjectName = null);
