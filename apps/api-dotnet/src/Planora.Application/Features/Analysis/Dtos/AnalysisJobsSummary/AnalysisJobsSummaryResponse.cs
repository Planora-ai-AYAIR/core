namespace Planora.Application.Features.Analysis.Dtos.AnalysisJobsSummary;

public sealed record AnalysisJobsSummaryResponse(
    int Total,
    int Completed,
    int Running,
    int Failed,
    IReadOnlyList<AnalysisJobSummaryItem> Analysis);

public sealed record AnalysisJobSummaryItem(
    Guid Id,
    Guid ParcelId,
    string Name,
    string Status,
    IReadOnlyList<string> Modules,
    DateTime Date);
