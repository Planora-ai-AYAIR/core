using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Options;
using Planora.Application.Features.Reports.Commands.SubmitPdfJob;
using Planora.Application.Features.Reports.Dtos.SubmitPdfJob;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Reports;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitPdfJob;

public sealed class SubmitReportHandler(
    IParcelRepository parcelRepository,
    IReportRepository reportRepository,
    IAnalysisJobRepository analysisJobRepository,
    IGeneratePdfJob generatePdfJob,
    ILogger<SubmitReportHandler> logger)
    : IRequestHandler<SubmitReportCommand, Result<SubmitReportResponse>>
{
    public async Task<Result<SubmitReportResponse>> Handle(
        SubmitReportCommand request,
        CancellationToken ct)
    {
        // Validate parcel exists
        var parcel =
            await parcelRepository.GetByIdAsync(
                request.ParcelId,
                ct);

        if (parcel is null)
        {
            return ParcelErrors.NotFound;
        }

        // making sure a completed analysis job exists for the parcel before generating a report
        var analysisJob =
        await analysisJobRepository
            .GetLatestCompletedByParcelIdAsync(
                request.ParcelId,
                ct);

        if (analysisJob is null)
        {
            return AnalysisJobErrors.NotFound;
        }

        // Prevent duplicate report generation
        var existingReport =
            await reportRepository.GetInProgressReportByParcelIdAsync(
                request.ParcelId,
                ct);

        if (existingReport is not null)
        {
            return ReportErrors.AlreadyRunning;
        }

        // Create report aggregate
        var reportResult =
            Report.Create(
                Guid.NewGuid(),
                request.ParcelId,
                parcel.UserId,
                ReportTier.Free,
                1);

        if (reportResult.IsError)
        {
            return reportResult.TopError;
        }

        var report = reportResult.Value;

        // Create PDF module
        var pdfModule =
            ReportModule.Create(
                Guid.NewGuid(),
                report.Id,
                ModuleType.PdfReport);

        report.AddModule(pdfModule);

        // Persist report first
        await reportRepository.AddAsync(
            report,
            ct);

        // Schedule Hangfire job
        var options = new ReportGenerationOptions(
                request.Language,
                request.CompanyName,
                request.ProjectName,
                request.IncludeMaps,
                request.IncludeTables,
                request.IncludeRiskMatrix,
                request.IncludeBoreholePlan,
                request.DisclaimerLevel);

        var jobId = generatePdfJob.Enqueue(report.Id, options);

        // Update report status
        report.MarkAsQueued(jobId);

        // Persist status changes
        await reportRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Report {ReportId} queued successfully with Hangfire JobId {JobId}",
            report.Id,
            jobId);

        // Return API response
        return new SubmitReportResponse(
            report.Id,
            report.ParcelId,
            ReportStatus.Queued.ToString(),
            DateTime.UtcNow,
            "30-60 seconds");
    }
}