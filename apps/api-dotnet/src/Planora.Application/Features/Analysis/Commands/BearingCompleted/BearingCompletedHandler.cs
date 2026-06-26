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

namespace Planora.Application.Features.Analysis.Commands.BearingCompleted;

public sealed class BearingCompletedHandler(
    IAnalysisJobRepository analysisJobRepository,
    ISoilResultRepository soilResultRepository,
    IParcelRepository parcelRepository,
    INotificationRepository notificationRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<BearingCompletedHandler> logger) : IRequestHandler<BearingCompletedCommand, Result<AnalysisJobProcessedResponse>>
{
    public async Task<Result<AnalysisJobProcessedResponse>> Handle(BearingCompletedCommand request, CancellationToken ct)
    {
        logger.LogInformation("Processing bearing completed webhook for PythonJobId: {PythonJobId}", request.PythonJobId);

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

        var payload = request.Payload;
        var featureImportanceJson = payload.FeatureImportance is not null
            ? JsonSerializer.Serialize(payload.FeatureImportance) : null;
        var soilFactorsJson = payload.SoilFactors is not null
            ? JsonSerializer.Serialize(payload.SoilFactors) : null;

        var existingSoilResult = await soilResultRepository.GetByAnalysisJobIdAsync(analysisJob.Id, ct);

        if (existingSoilResult is not null)
        {
            existingSoilResult.SetBearingResult(
                payload.BearingCapacityKpa,
                payload.Classification ?? "",
                bearingConfidence: payload.Confidence,
                bearingRange: payload.Range,
                bearingTrafficLight: payload.TrafficLight,
                recommendedFoundation: payload.RecommendedFoundation,
                maxFloorsWithoutDeepFoundation: payload.MaxFloorsWithoutDeepFoundation,
                floorCountCategory: payload.FloorCountCategory,
                bearingMinKpa: payload.UncertaintyRange?.MinimumKpa,
                bearingMaxKpa: payload.UncertaintyRange?.MaximumKpa,
                featureImportanceJson: featureImportanceJson,
                soilFactorsJson: soilFactorsJson,
                bearingModelName: payload.ModelMetadata?.ModelName,
                bearingFramework: payload.ModelMetadata?.Framework,
                bearingTrainingR2: payload.ModelMetadata?.TrainingR2,
                bearingShapEnabled: payload.ModelMetadata?.ShapEnabled);
        }
        else
        {
            var soilResult = new SoilResult(
                analysisJob.Id, 0, 0, 0, 0, 0, 0,
                payload.BearingCapacityKpa, payload.Classification ?? "",
                bearingConfidence: payload.Confidence,
                bearingRange: payload.Range,
                bearingTrafficLight: payload.TrafficLight,
                recommendedFoundation: payload.RecommendedFoundation,
                maxFloorsWithoutDeepFoundation: payload.MaxFloorsWithoutDeepFoundation,
                floorCountCategory: payload.FloorCountCategory,
                bearingMinKpa: payload.UncertaintyRange?.MinimumKpa,
                bearingMaxKpa: payload.UncertaintyRange?.MaximumKpa,
                featureImportanceJson: featureImportanceJson,
                soilFactorsJson: soilFactorsJson,
                bearingModelName: payload.ModelMetadata?.ModelName,
                bearingFramework: payload.ModelMetadata?.Framework,
                bearingTrainingR2: payload.ModelMetadata?.TrainingR2,
                bearingShapEnabled: payload.ModelMetadata?.ShapEnabled);

            await soilResultRepository.AddAsync(soilResult, ct);
        }

        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await AnalysisNotificationHelper.PublishCompletionNotificationAsync(
            analysisJob, parcelRepository, notificationRepository, notificationPublisher, ct);

        logger.LogInformation("Successfully processed bearing completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
