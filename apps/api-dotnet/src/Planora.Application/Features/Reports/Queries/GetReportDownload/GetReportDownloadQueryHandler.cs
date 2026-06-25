using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Reports.Dtos.GetReportDownload;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Enums;
using Planora.Domain.Reports;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Reports.Queries.GetReportDownload;

public sealed class GetReportDownloadQueryHandler(
    IReportRepository reportRepository,
    IStorageService storageService,
    ILogger<GetReportDownloadQueryHandler> logger)
    : IRequestHandler<GetReportDownloadQuery, Result<GetReportDownloadResponse>>
{
    private static readonly TimeSpan UrlExpiry = TimeSpan.FromHours(1);

    public async Task<Result<GetReportDownloadResponse>> Handle(
        GetReportDownloadQuery request,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Fetching report download for ReportJobId: {ReportJobId}",
            request.ReportId);

        // 1. Retrieve report with modules and files
        var report = await reportRepository.GetByIdAsync(request.ReportId, ct);

        if (report is null)
        {
            logger.LogWarning(
                "Report not found for ReportJobId: {ReportJobId}",
                request.ReportId);

            return ReportErrors.NotFound;
        }

        // 2. Check if report is completed
        if (report.Status != ReportStatus.Completed)
        {
            logger.LogInformation(
                "Report {ReportId} is not ready yet. Status: {Status}",
                request.ReportId,
                report.Status);

            return ReportErrors.NotReady;
        }

        // 3. Get PDF module
        var pdfModule = report.Modules
            .FirstOrDefault(m => m.ModuleType == ModuleType.PdfReport);

        if (pdfModule is null || string.IsNullOrWhiteSpace(pdfModule.OutputS3Key))
        {
            logger.LogError(
                "PDF module missing or has no S3 key. ReportId: {ReportId}",
                request.ReportId);

            return ReportErrors.PdfModuleMissing;
        }

        // 4. Generate presigned URL with 1-hour expiry
        var downloadUrl = await storageService.GetPreSignedUrlAsync(
            pdfModule.OutputS3Key,
            UrlExpiry);

        var expiresAt = DateTime.UtcNow.Add(UrlExpiry);

        // 5. Get filename from ReportFile or build fallback
        var reportFile = report.Files
            .FirstOrDefault(f => f.FileType == FileType.Pdf);

        var filename = reportFile?.FileName
            ?? $"Planora_Report_{report.ParcelId}_{DateTime.UtcNow:yyyyMMdd}.pdf";

        logger.LogInformation(
            "Presigned URL generated for ReportId: {ReportId}, expires at: {ExpiresAt}",
            request.ReportId,
            expiresAt);

        // 6. Build response
        return new GetReportDownloadResponse(
            report.Id,
            report.ParcelId,
            report.Status.ToString(),
            downloadUrl,
            expiresAt,
            filename,
            pdfModule.FileSizeBytes,
            pdfModule.PageCount);
    }
}