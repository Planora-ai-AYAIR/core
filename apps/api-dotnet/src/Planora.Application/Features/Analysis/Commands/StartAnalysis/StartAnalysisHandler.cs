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
    IProcessTopographyJob processTopographyJob,
    IProcessSoilJob processSoilJob,
    IProcessBearingJob processBearingJob,
    IProcessRiskJob processRiskJob,
    IProcessBoreholeJob processBoreholeJob,
    INotificationRepository notificationRepository,
    INotificationPublisher notificationPublisher,
    IHybridCacheService cacheService,
    ILogger<StartAnalysisHandler> logger) : IRequestHandler<StartAnalysisCommand, Result<StartAnalysisResponse>>
{
    public async Task<Result<StartAnalysisResponse>> Handle(StartAnalysisCommand request, CancellationToken ct)
    {
        logger.LogInformation("Starting per-module analysis for ParcelId {ParcelId}", request.ParcelId);

        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);
        if (parcel is null)
            return ParcelErrors.NotFound;

        var hasActiveJob = await analysisJobRepository.HasActiveJobAsync(request.ParcelId, ct);
        if (hasActiveJob)
            return AnalysisJobErrors.AlreadyRunning;

        var modulesToRun = ResolveModules(request.Options);
        if (modulesToRun.Count == 0)
            return AnalysisJobErrors.UnsupportedEventType;

        var sharedOptions = BuildOptions(request.Options);
        var createdJobs = new List<AnalysisJob>(modulesToRun.Count);

        foreach (var moduleType in modulesToRun)
        {
            var createResult = AnalysisJob.Create(
                id: Guid.NewGuid(),
                parcelId: parcel.Id,
                pythonJobId: BuildPendingPythonJobId(moduleType),
                type: moduleType,
                options: sharedOptions);

            if (createResult.IsError)
            {
                logger.LogError(
                    "Failed to create AnalysisJob row for ParcelId {ParcelId}, Module {Module}: {Error}",
                    parcel.Id, moduleType, createResult.TopError.Description);
                return createResult.Errors;
            }

            await analysisJobRepository.AddAsync(createResult.Value, ct);
            createdJobs.Add(createResult.Value);
        }

        foreach (var job in createdJobs)
        {
            var hangfireJobId = EnqueueModule(job.Type, parcel.Id, job.Id);

            logger.LogInformation(
                "Enqueued {Module} job. ParcelId {ParcelId}, AnalysisJobId {AnalysisJobId}, HangfireJobId {HangfireJobId}",
                job.Type, parcel.Id, job.Id, hangfireJobId);
        }

        await cacheService.SetAsync(
            $"parcel-status:{parcel.Id}",
            ParcelStatus.Processing,
            ct: ct);

        parcel.MarkAsProcessing();
        await parcelRepository.UpdateAsync(parcel, ct);

        foreach (var job in createdJobs)
        {
            await AnalysisNotificationHelper.PublishStartedNotificationAsync(
                job, parcelRepository, notificationRepository, notificationPublisher, ct);
        }

        var primaryJob = createdJobs[0];

        return new StartAnalysisResponse(
            AnalysisJobId: $"ANL-{primaryJob.Id:N}",
            ParcelId: parcel.Id.ToString(),
            Status: "Processing",
            SubmittedAt: DateTime.UtcNow,
            EstimatedDuration: "2-6 hours",
            PollEndpoint: $"/api/parcels/{parcel.Id}/analysis-status");
    }

    private string EnqueueModule(AnalysisType type, Guid parcelId, Guid analysisJobId) => type switch
    {
        AnalysisType.Topography => processTopographyJob.Enqueue(parcelId, analysisJobId),
        AnalysisType.Soil       => processSoilJob.Enqueue(parcelId, analysisJobId),
        AnalysisType.Bearing   => processBearingJob.Enqueue(parcelId, analysisJobId),
        AnalysisType.Risk       => processRiskJob.Enqueue(parcelId, analysisJobId),
        AnalysisType.Borehole   => processBoreholeJob.Enqueue(parcelId, analysisJobId),
        _ => throw new InvalidOperationException($"No background job mapped for AnalysisType {type}.")
    };

    private static List<AnalysisType> ResolveModules(AnalysisOptionsDto options)
    {
        var modules = new List<AnalysisType>(5);
        if (options.IncludeTopography) modules.Add(AnalysisType.Topography);
        if (options.IncludeSoil)        modules.Add(AnalysisType.Soil);
        if (options.IncludeBearing)     modules.Add(AnalysisType.Bearing);
        if (options.IncludeRisk)        modules.Add(AnalysisType.Risk);
        if (options.IncludeBorehole)    modules.Add(AnalysisType.Borehole);
        return modules;
    }

    private static AnalysisOptions BuildOptions(AnalysisOptionsDto dto) =>
        new(
            dto.IncludeTopography,
            dto.IncludeSoil,
            dto.IncludeBearing,
            dto.IncludeRisk,
            dto.IncludeBorehole,
            dto.ContourInterval,
            dto.SlopeCategories is not null ? JsonSerializer.Serialize(dto.SlopeCategories) : null,
            dto.ReferencePlane,
            dto.SoilDepths is not null ? JsonSerializer.Serialize(dto.SoilDepths) : null);

    private static string BuildPendingPythonJobId(AnalysisType type) =>
        $"pending-{type.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}";
}
