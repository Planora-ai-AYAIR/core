namespace Planora.Application.Features.Parcels.Dtos.PdfReport;

public sealed record PdfReportResponse(
    Guid ParcelId,
    string DownloadUrl,
    int? PageCount,
    long? SizeBytes,
    DateTime? GeneratedAt
);
