namespace Planora.Application.Features.Reports.Dtos.SubmitPdfJob;

public sealed class ReportPdfData
{
    public Guid ReportId { get; set; }
    public Guid ParcelId { get; set; }
    public string? CompanyName { get; set; }
    public string? ProjectName { get; set; }
    public string? UserFullName { get; set; }
    public string Language { get; set; } = "en";
    public bool IncludeMaps { get; set; }
    public bool IncludeTables { get; set; }
    public bool IncludeRiskMatrix { get; set; }
    public bool IncludeBoreholePlan { get; set; }
    public string? DisclaimerLevel { get; set; }

    // This is your AggregatedAnalysisData
    public AggregatedAnalysisData Analysis { get; set; } = new();
}