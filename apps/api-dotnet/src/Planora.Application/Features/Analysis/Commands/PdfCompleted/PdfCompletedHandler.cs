using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Analysis.Dtos;
using Planora.Application.Features.Parcels.Dtos.Webhook;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Shared.Results;
using INotificationPublisher = Planora.Application.Interfaces.Services.INotificationPublisher;

namespace Planora.Application.Features.Analysis.Commands.PdfCompleted;

public sealed class PdfCompletedHandler(
    IAnalysisJobRepository analysisJobRepository,
    IReportRepository reportRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<PdfCompletedHandler> logger) : IRequestHandler<PdfCompletedCommand, Result<AnalysisJobProcessedResponse>>
{
    public async Task<Result<AnalysisJobProcessedResponse>> Handle(PdfCompletedCommand request, CancellationToken ct)
    {
        logger.LogInformation("Processing PDF completed webhook for PythonJobId: {PythonJobId}", request.PythonJobId);

        var analysisJob = await analysisJobRepository.GetByPythonJobIdAsync(request.PythonJobId, ct);

        if (analysisJob is null)
        {
            logger.LogWarning("AnalysisJob not found for PythonJobId: {PythonJobId}", request.PythonJobId);
            return AnalysisJobErrors.NotFound;
        }

        if (analysisJob.Status != AnalysisJobStatus.Running)
        {
            logger.LogWarning("Invalid state transition for AnalysisJob {AnalysisJobId}. Current status: {Status}", analysisJob.Id, analysisJob.Status);
            return AnalysisJobErrors.InvalidStateTransition;
        }

        var markAsCompletedResult = analysisJob.MarkAsCompleted();
        if (markAsCompletedResult.IsError)
        {
            logger.LogError("Failed to update status for AnalysisJob {AnalysisJobId}", analysisJob.Id);
            return AnalysisJobErrors.FaildStatusUpdate;
        }

        // Update the PDF module in the associated report
        var report = await reportRepository.GetInProgressReportByParcelIdAsync(analysisJob.ParcelId, ct);

        if (report is not null)
        {
            var pdfModule = report.Modules.FirstOrDefault(m => m.ModuleType == ModuleType.PdfReport);
            if (pdfModule is not null && !string.IsNullOrWhiteSpace(request.Payload.PdfS3Url))
            {
                pdfModule.SetOutput(request.Payload.PdfS3Url, request.Payload.PageCount, request.Payload.SizeBytes);
            }
        }

        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await AnalysisNotificationHelper.PublishAnalysisResultAsync(
            analysisJob, AiWebhookEventTypes.PdfCompleted, request.Payload, notificationPublisher, ct);

        logger.LogInformation("Successfully processed PDF completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
