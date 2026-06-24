namespace Planora.Application.Features.Reports.Dtos.SubmitPdfJob;

public sealed record SubmitReportResponse(
    Guid ReportId,
    Guid ParcelId,
    string Status,
    DateTime SubmittedAt,
    string EstimatedDuration
);
