using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Parcels.Dtos.SubmitTopographyJob;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitTopographyJob;

public sealed class SubmitTopographyJobHandler(
    IParcelRepository parcelRepository,
    IAnalysisJobRepository analysisJobRepository,
    IHybridCacheService cacheService,
    IProcessTopographyJob processTopographyJob,
    ILogger<SubmitTopographyJobHandler> logger)
    : IRequestHandler<SubmitTopographyJobCommand, Result<SubmitTopographyJobResponse>>
{
    public async Task<Result<SubmitTopographyJobResponse>> Handle(SubmitTopographyJobCommand request, CancellationToken ct)
    {
        logger.LogInformation("Submitting topography job for ParcelId {ParcelId}", request.ParcelId);

        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);
        if (parcel is null)
            return ParcelErrors.NotFound;

        if (await analysisJobRepository.HasActiveJobAsync(request.ParcelId, ct))
            return AnalysisJobErrors.AlreadyRunning;

        var createResult = AnalysisJob.Create(
            id: Guid.NewGuid(),
            parcelId: parcel.Id,
            pythonJobId: $"pending-topography-{Guid.NewGuid():N}",
            type: AnalysisType.Topography);

        if (createResult.IsError)
            return createResult.Errors;

        await analysisJobRepository.AddAsync(createResult.Value, ct);

        var hangfireJobId = processTopographyJob.Enqueue(parcel.Id, createResult.Value.Id);

        await cacheService.SetAsync(
            $"parcel-status:{parcel.Id}",
            ParcelStatus.Processing,
            ct: ct);

        parcel.MarkAsProcessing();
        await parcelRepository.UpdateAsync(parcel, ct);

        return new SubmitTopographyJobResponse(
            hangfireJobId,
            parcel.Id,
            ParcelStatus.Queued.ToString(),
            DateTime.UtcNow);
    }
}
