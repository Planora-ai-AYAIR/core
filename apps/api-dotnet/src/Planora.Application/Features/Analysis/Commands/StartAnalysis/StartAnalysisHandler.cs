using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Analysis.Dtos.StartAnalysis;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;
using INotificationPublisher = Planora.Application.Interfaces.Services.INotificationPublisher;

namespace Planora.Application.Features.Analysis.Commands.StartAnalysis;

public sealed class StartAnalysisHandler(
    IParcelRepository parcelRepository,
    IAnalysisJobRepository analysisJobRepository,
    IProcessAggregatedAnalysisJob processAggregatedAnalysisJob,
    INotificationRepository notificationRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<StartAnalysisHandler> logger) : IRequestHandler<StartAnalysisCommand, Result<StartAnalysisResponse>>
{
    public async Task<Result<StartAnalysisResponse>> Handle(StartAnalysisCommand request, CancellationToken ct)
    {
        logger.LogInformation($"Starting aggregated analysis for ParcelId {request.ParcelId}", request.ParcelId);

        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);
        if (parcel is null)
            return ParcelErrors.NotFound;

        var hasActiveJob = await analysisJobRepository.HasActiveJobAsync(request.ParcelId, ct);
        if (hasActiveJob)
            return AnalysisJobErrors.AlreadyRunning;

        var options = new AnalysisOptions(
            request.Options.IncludeTopography,
            request.Options.IncludeSoil,
            request.Options.IncludeBearing,
            request.Options.IncludeRisk,
            request.Options.IncludeBorehole,
            request.Options.ContourInterval,
            request.Options.SlopeCategories is not null ? JsonSerializer.Serialize(request.Options.SlopeCategories) : null,
            request.Options.ReferencePlane,
            request.Options.SoilDepths is not null ? JsonSerializer.Serialize(request.Options.SoilDepths) : null);

        var analysisJobResult = AnalysisJob.Create(
            id: Guid.NewGuid(),
            parcelId: parcel.Id,
            pythonJobId: $"pending-{Guid.NewGuid():N}",
            type: AnalysisType.Aggregated,
            options: options);

        if (analysisJobResult.IsError)
            return analysisJobResult.Errors;

        await analysisJobRepository.AddAsync(analysisJobResult.Value, ct);

        var hangfireJobId = processAggregatedAnalysisJob.Enqueue(parcel.Id, analysisJobResult.Value.Id);

        await cacheService.SetAsync(
            $"parcel-status:{parcel.Id}",
            ParcelStatus.Processing,
            ct: ct);

        parcel.MarkAsProcessing();
        await parcelRepository.UpdateAsync(parcel, ct);

        await AnalysisNotificationHelper.PublishStartedNotificationAsync(
            analysisJobResult.Value, parcelRepository, notificationRepository, notificationPublisher, ct);

        logger.LogInformation(
            "Aggregated analysis started for ParcelId {ParcelId}, AnalysisJobId {AnalysisJobId}, HangfireJobId {HangfireJobId}",
            request.ParcelId, analysisJobResult.Value.Id, hangfireJobId);

        return new StartAnalysisResponse(
            AnalysisJobId: $"ANL-{analysisJobResult.Value.Id:N}",
            ParcelId: parcel.Id.ToString(),
            Status: "Processing",
            SubmittedAt: DateTime.UtcNow,
            EstimatedDuration: "2-6 hours",
            PollEndpoint: $"/api/parcels/{parcel.Id}/analysis-status");
    }
}
