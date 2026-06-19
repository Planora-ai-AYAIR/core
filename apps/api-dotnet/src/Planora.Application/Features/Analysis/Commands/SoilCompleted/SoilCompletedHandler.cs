using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Analysis.Dtos;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Analysis;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;
using System.Text.Json;
using INotificationPublisher = Planora.Application.Interfaces.Services.INotificationPublisher;

namespace Planora.Application.Features.Analysis.Commands.SoilCompleted;

public sealed class SoilCompletedHandler(
    IAnalysisJobRepository analysisJobRepository,
    ISoilResultRepository soilResultRepository,
    IParcelRepository parcelRepository,
    INotificationRepository notificationRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<SoilCompletedHandler> logger) : IRequestHandler<SoilCompletedCommand, Result<AnalysisJobProcessedResponse>>
{
    public async Task<Result<AnalysisJobProcessedResponse>> Handle(SoilCompletedCommand request, CancellationToken ct)
    {
        logger.LogInformation("Processing soil completed webhook for PythonJobId: {PythonJobId}", request.PythonJobId);

        var analysisJob = await analysisJobRepository.GetByPythonJobIdAsync(request.PythonJobId, ct);

        if (analysisJob is null)
        {
            logger.LogWarning("AnalysisJob not found for PythonJobId: {PythonJobId}", request.PythonJobId);
            return AnalysisJobErrors.NotFound;
        }

        var markAsCompletedResult = analysisJob.MarkAsCompleted();
        if (markAsCompletedResult.IsError)
        {
            logger.LogError("Failed to update status for AnalysisJob {AnalysisJobId}", analysisJob.Id);
            return AnalysisJobErrors.FaildStatusUpdate;
        }

        var multiDepthProfileJson = request.Payload.DepthProfiles is not null
            ? JsonSerializer.Serialize(request.Payload.DepthProfiles)
            : null;

        var soilResult = new SoilResult(
            analysisJob.Id,
            request.Payload.SandPercent,
            request.Payload.SiltPercent,
            request.Payload.ClayPercent,
            request.Payload.BulkDensity,
            request.Payload.OrganicCarbon,
            request.Payload.Ph,
            request.Payload.BearingCapacityEstimate,
            request.Payload.BearingCapacityCategory,
            request.Payload.CompositionUnit,
            request.Payload.BulkDensityUnit,
            request.Payload.OrganicCarbonUnit,
            request.Payload.PrimaryType,
            request.Payload.UsdaClass,
            request.Payload.AiConfidence,
            multiDepthProfileJson,
            request.Payload.HeatmapTileUrl);

        await soilResultRepository.AddAsync(soilResult, ct);
        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await AnalysisNotificationHelper.PublishCompletionNotificationAsync(
            analysisJob, parcelRepository, notificationRepository, notificationPublisher, ct);

        logger.LogInformation("Successfully processed soil completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
