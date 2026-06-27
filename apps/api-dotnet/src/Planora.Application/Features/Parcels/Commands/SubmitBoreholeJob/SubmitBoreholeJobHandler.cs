using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Parcels.Dtos.SubmitBoreholeJob;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitBoreholeJob;

public sealed class SubmitBoreholeJobHandler(
    IParcelRepository parcelRepository,
    IAnalysisJobRepository analysisJobRepository,
    IHybridCacheService cacheService,
    IProcessBoreholeJob processBoreholeJob,
    ILogger<SubmitBoreholeJobHandler> logger)
    : IRequestHandler<SubmitBoreholeJobCommand, Result<SubmitBoreholeJobResponse>>
{
    public async Task<Result<SubmitBoreholeJobResponse>> Handle(SubmitBoreholeJobCommand request, CancellationToken ct)
    {
        logger.LogInformation("Submitting borehole job for ParcelId {ParcelId}", request.ParcelId);

        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);
        if (parcel is null)
            return ParcelErrors.NotFound;

        if (await analysisJobRepository.HasActiveJobAsync(request.ParcelId, ct))
            return AnalysisJobErrors.AlreadyRunning;

        var createResult = AnalysisJob.Create(
            id: Guid.NewGuid(),
            parcelId: parcel.Id,
            pythonJobId: $"pending-borehole-{Guid.NewGuid():N}",
            type: AnalysisType.Borehole);

        if (createResult.IsError)
            return createResult.Errors;

        await analysisJobRepository.AddAsync(createResult.Value, ct);

        var hangfireJobId = processBoreholeJob.Enqueue(parcel.Id, createResult.Value.Id);

        await cacheService.SetAsync(
            $"parcel-status:{parcel.Id}",
            ParcelStatus.Processing,
            ct: ct);

        parcel.MarkAsProcessing();
        await parcelRepository.UpdateAsync(parcel, ct);

        return new SubmitBoreholeJobResponse(
            hangfireJobId,
            parcel.Id,
            ParcelStatus.Queued.ToString(),
            DateTime.UtcNow);
    }
}
