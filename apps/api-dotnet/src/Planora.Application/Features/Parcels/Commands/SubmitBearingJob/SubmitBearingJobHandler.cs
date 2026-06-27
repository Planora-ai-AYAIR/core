using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Parcels.Dtos.SubmitBearingJob;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitBearingJob;

public sealed class SubmitBearingJobHandler(
    IParcelRepository parcelRepository,
    IAnalysisJobRepository analysisJobRepository,
    IHybridCacheService cacheService,
    IProcessBearingJob processBearingJob,
    ILogger<SubmitBearingJobHandler> logger)
    : IRequestHandler<SubmitBearingJobCommand, Result<SubmitBearingJobResponse>>
{
    public async Task<Result<SubmitBearingJobResponse>> Handle(SubmitBearingJobCommand request, CancellationToken ct)
    {
        logger.LogInformation("Submitting bearing job for ParcelId {ParcelId}", request.ParcelId);

        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);
        if (parcel is null)
            return ParcelErrors.NotFound;

        if (await analysisJobRepository.HasActiveJobAsync(request.ParcelId, ct))
            return AnalysisJobErrors.AlreadyRunning;

        var createResult = AnalysisJob.Create(
            id: Guid.NewGuid(),
            parcelId: parcel.Id,
            pythonJobId: $"pending-bearing-{Guid.NewGuid():N}",
            type: AnalysisType.Bearing);

        if (createResult.IsError)
            return createResult.Errors;

        await analysisJobRepository.AddAsync(createResult.Value, ct);

        var hangfireJobId = processBearingJob.Enqueue(parcel.Id, createResult.Value.Id);

        await cacheService.SetAsync(
            $"parcel-status:{parcel.Id}",
            ParcelStatus.Processing,
            ct: ct);

        parcel.MarkAsProcessing();
        await parcelRepository.UpdateAsync(parcel, ct);

        return new SubmitBearingJobResponse(
            hangfireJobId,
            parcel.Id,
            ParcelStatus.Queued.ToString(),
            DateTime.UtcNow);
    }
}
