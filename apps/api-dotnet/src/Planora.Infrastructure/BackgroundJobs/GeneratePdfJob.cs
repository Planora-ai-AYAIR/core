// Planora.Infrastructure/BackgroundJobs/GeneratePdfJob.cs
using Hangfire;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Options;
using Planora.Application.Features.Reports.Dtos;
using Planora.Application.Features.Reports.Dtos.SubmitPdfJob;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Enums;
using Planora.Domain.Reports;

namespace Planora.Infrastructure.BackgroundJobs;

public sealed class GeneratePdfJob(
    IBackgroundJobClient backgroundJobClient,
    IReportRepository reportRepository,
    IAnalysisResultQuery analysisResultQuery,
    IUserRepository userRepository,
    IPdfGeneratorService pdfGenerator,
    IStorageService storageService,
    IReportNotifier reportNotifier,
    ILogger<GeneratePdfJob> logger)
    : IGeneratePdfJob
{
    public string Enqueue(Guid reportId, ReportGenerationOptions options)
    {
        var jobId = backgroundJobClient.Enqueue<GeneratePdfJob>(
            x => x.Execute(reportId, options, CancellationToken.None));

        logger.LogInformation(
            "PDF generation job {JobId} queued for ReportId {ReportId}",
            jobId, reportId);

        return jobId;
    }

    public async Task Execute(
        Guid reportId,
        ReportGenerationOptions options,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Starting PDF generation for ReportId {ReportId}",
            reportId);

        try
        {
            // 1. Load report with modules and files
            var report = await reportRepository.GetByIdAsync(reportId, ct);
            if (report is null)
            {
                logger.LogError("Report {ReportId} not found", reportId);
                return;
            }

            // 2. Mark as Processing
            report.MarkAsProcessing();
            await reportRepository.SaveChangesAsync(ct);

            // 3. Load user for CompanyName/ProjectName fallback
            var user = await userRepository.FindByIdAsync(report.UserId, ct);
            var companyName = options.CompanyName ?? user?.CompanyName;
            var projectName = options.ProjectName ?? user?.ProjectName;
            var userFullName = user != null ? $"{user.FirstName} {user.LastName}" : null;

            // 4. Load aggregated analysis data (as mutable DTOs)
            var analysisData = await analysisResultQuery.GetByParcelIdAsync(report.ParcelId, ct);
            if (analysisData is null)
            {
                throw new InvalidOperationException(
                    $"No completed analysis found for ParcelId {report.ParcelId}");
            }

            // 5. Generate presigned URLs for map images (if requested)
            if (options.IncludeMaps)
            {
                await EnrichWithPresignedUrlsAsync(analysisData, ct);
            }

            // 6. Build PDF data
            var pdfData = new ReportPdfData
            {
                ReportId = report.Id,
                ParcelId = report.ParcelId,
                CompanyName = companyName,
                ProjectName = projectName,
                UserFullName = userFullName,
                Language = options.Language ?? "en",
                IncludeMaps = options.IncludeMaps,
                IncludeTables = options.IncludeTables,
                IncludeRiskMatrix = options.IncludeRiskMatrix,
                IncludeBoreholePlan = options.IncludeBoreholePlan,
                DisclaimerLevel = options.DisclaimerLevel,
                Analysis = analysisData
            };

            // 7. Generate PDF in-memory (no disk write)
            var pdfBytes = await pdfGenerator.GenerateAsync(pdfData, ct);

            // 8. Upload to S3
            var s3Key = $"reports/{report.ParcelId}/{report.Id}.pdf";
            var s3Uri = await storageService.UploadAsync(pdfBytes, s3Key, "application/pdf", ct);

            logger.LogInformation("PDF uploaded to {S3Uri}", s3Uri);

            // 9. Create ReportFile record
            var fileName = $"Planora_Report_{report.ParcelId}_{DateTime.UtcNow:yyyyMMdd}.pdf";
            var reportFile = ReportFile.Create(
                Guid.NewGuid(),
                report.Id,
                FileType.Pdf,
                fileName,
                s3Key,
                "application/pdf",
                pdfBytes.Length);

            report.AddFile(reportFile);

            // 10. Update PDF module with output
            var pdfModule = report.Modules.FirstOrDefault(m => m.ModuleType == ModuleType.PdfReport);
            pdfModule?.SetOutput(s3Key, pageCount: null, pdfBytes.Length);

            // 11. Mark report completed
            report.MarkAsCompleted();
            await reportRepository.SaveChangesAsync(ct);

            // 12. Notify via SignalR
            await reportNotifier.NotifyReportGeneratedAsync(report.ParcelId, report.Id, ct);

            logger.LogInformation(
                "PDF generation completed for ReportId {ReportId}. S3Uri: {S3Uri}",
                reportId, s3Uri);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PDF generation failed for ReportId {ReportId}", reportId);

            var report = await reportRepository.GetByIdAsync(reportId, ct);
            if (report is not null)
            {
                report.MarkAsFailed(ex.Message);
                await reportRepository.SaveChangesAsync(ct);
                await reportNotifier.NotifyReportFailedAsync(
                    report.ParcelId, reportId, ex.Message, ct);
            }
        }
    }

    /// <summary>
    /// Converts AI S3 asset URLs to presigned URLs for embedding in PDF
    /// </summary>
    private async Task EnrichWithPresignedUrlsAsync(AggregatedAnalysisData data, CancellationToken ct)
    {
        // Topography
        if (data.Topography is not null)
        {
            data.Topography.ElevationTileUrl = await PresignAsync(data.Topography.ElevationTileUrl, ct);
            data.Topography.SlopeTileUrl = await PresignAsync(data.Topography.SlopeTileUrl, ct);
            data.Topography.ContourGeoJsonUrl = await PresignAsync(data.Topography.ContourGeoJsonUrl, ct);
            data.Topography.PondingGeoJsonUrl = await PresignAsync(data.Topography.PondingGeoJsonUrl, ct);
        }

        // Soil
        if (data.Soil is not null)
        {
            data.Soil.HeatmapTileUrl = await PresignAsync(data.Soil.HeatmapTileUrl, ct);
        }

        // Risk
        if (data.Risk is not null)
        {
            data.Risk.FloodGeoJsonUrl = await PresignAsync(data.Risk.FloodGeoJsonUrl, ct);
        }

        // Borehole
        if (data.Borehole is not null)
        {
            data.Borehole.PlacementGeoJsonUrl = await PresignAsync(data.Borehole.PlacementGeoJsonUrl, ct);
        }
    }

    /// <summary>
    /// Extracts bucket and key from S3 URL and generates presigned URL
    /// </summary>
    private async Task<string?> PresignAsync(string? url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var uri = new Uri(url);
        var bucket = uri.Host.Split('.')[0];
        var key = uri.AbsolutePath.TrimStart('/');

        return await storageService.GetPreSignedUrlAsync(bucket, key, TimeSpan.FromHours(1));
    }
}