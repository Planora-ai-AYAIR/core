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

namespace Planora.Application.Features.Analysis.Commands.BoreholeCompleted;

public sealed class BoreholeCompletedHandler(
    IAnalysisJobRepository analysisJobRepository,
    IBoreholeResultRepository boreholeResultRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<BoreholeCompletedHandler> logger) : IRequestHandler<BoreholeCompletedCommand, Result<AnalysisJobProcessedResponse>>
{
    public async Task<Result<AnalysisJobProcessedResponse>> Handle(BoreholeCompletedCommand request, CancellationToken ct)
    {
        logger.LogInformation("Processing borehole completed webhook for PythonJobId: {PythonJobId}", request.PythonJobId);

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

        var recommendation = request.Payload.Recommendation;
        var costAnalysis = request.Payload.CostAnalysis;
        var traditional = costAnalysis?.TraditionalApproach;
        var optimized = costAnalysis?.OptimizedApproach;
        var savings = costAnalysis?.Savings;
        var assets = request.Payload.VisualizationAssets;

        var placementPointsJson = request.Payload.PlacementPoints is not null
            ? JsonSerializer.Serialize(request.Payload.PlacementPoints)
            : null;

        var boreholeResult = new BoreholeResult(
            analysisJob.Id,
            recommendation?.MinimumRequired ?? 0,
            recommendation?.OptimalCount ?? 0,
            recommendation?.CoveragePercentage ?? 0,
            recommendation?.GridSize,
            recommendation?.Strategy,
            placementPointsJson,
            assets?.BoreholePointsGeoJsonUrl,
            traditional?.Boreholes ?? 0,
            traditional?.EstimatedCost ?? 0,
            optimized?.Boreholes ?? 0,
            optimized?.EstimatedCost ?? 0,
            savings?.Amount ?? 0,
            savings?.Percentage ?? 0,
            savings?.Currency ?? traditional?.Currency ?? optimized?.Currency);

        await boreholeResultRepository.AddAsync(boreholeResult, ct);
        await analysisJobRepository.SaveChangesAsync(ct);

        await cacheService.RemoveByTagAsync($"parcel:{analysisJob.ParcelId}", ct);

        await AnalysisNotificationHelper.PublishAnalysisResultAsync(
            analysisJob, AiWebhookEventTypes.BoreholeCompleted, request.Payload, notificationPublisher, ct);

        logger.LogInformation("Successfully processed borehole completed webhook for AnalysisJob {AnalysisJobId}, PythonJobId: {PythonJobId}", analysisJob.Id, request.PythonJobId);

        return new AnalysisJobProcessedResponse(analysisJob.Id, request.PythonJobId);
    }
}
