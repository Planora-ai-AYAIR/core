using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Analysis.Dtos;
using Planora.Application.Features.Parcels.Dtos.Webhook;
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
    IBearingResultRepository bearingResultRepository,
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

        var existingBearingResult = await bearingResultRepository.GetByAnalysisJobIdAsync(analysisJob.Id, ct);

        if (existingBearingResult is not null)
        {
            existingBearingResult.Update(
                payload.BearingCapacityKpa,
                payload.Classification,
                confidence: payload.Confidence,
                range: payload.Range,
                trafficLight: payload.TrafficLight,
                recommendedFoundation: payload.RecommendedFoundation,
                maxFloorsWithoutDeepFoundation: payload.MaxFloorsWithoutDeepFoundation,
                floorCountCategory: payload.FloorCountCategory,
                minKpa: payload.UncertaintyRange?.MinimumKpa,
                maxKpa: payload.UncertaintyRange?.MaximumKpa,
                featureImportanceJson: featureImportanceJson,
                soilFactorsJson: soilFactorsJson,
                modelName: payload.ModelMetadata?.ModelName,
                framework: payload.ModelMetadata?.Framework,
                trainingR2: payload.ModelMetadata?.TrainingR2,
                shapEnabled: payload.ModelMetadata?.ShapEnabled);
        }
        else
        {
            var bearingResult = new BearingResult(
                analysisJob.Id,
                payload.BearingCapacityKpa,
                payload.Classification,
                confidence: payload.Confidence,
                range: payload.Range,
                trafficLight: payload.TrafficLight,
                recommendedFoundation: payload.RecommendedFoundation,
                maxFloorsWithoutDeepFoundation: payload.MaxFloorsWithoutDeepFoundation,
                floorCountCategory: payload.FloorCountCategory,
                minKpa: payload.UncertaintyRange?.MinimumKpa,
                maxKpa: payload.UncertaintyRange?.MaximumKpa,
                featureImportanceJson: featureImportanceJson,
                soilFactorsJson: soilFactorsJson,
                modelName: payload.ModelMetadata?.ModelName,
                framework: payload.ModelMetadata?.Framework,
                trainingR2: payload.ModelMetadata?.TrainingR2,
                shapEnabled: payload.ModelMetadata?.ShapEnabled);

            await bearingResultRepository.AddAsync(bearingResult, ct);
        }

        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await AnalysisNotificationHelper.PublishAnalysisResultAsync(
            analysisJob, AiWebhookEventTypes.BearingCompleted, request.Payload, notificationPublisher, ct);

        logger.LogInformation("Successfully processed bearing completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
