using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Parcels.Dtos.SubmitTopographyJob;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitTopographyJob;

public sealed class SubmitTopographyJobHandler(
        IParcelRepository parcelRepository,
        IHybridCacheService cacheService,
        IProcessTopographyJob processTopographyJob,
        ILogger<SubmitTopographyJobHandler> logger)
        : IRequestHandler<
            SubmitTopographyJobCommand,
            Result<SubmitTopographyJobResponse>>
{
    public async Task<Result<SubmitTopographyJobResponse>> Handle(SubmitTopographyJobCommand request,CancellationToken ct)
    {
        logger.LogInformation(
            "Submitting topography job for ParcelId {ParcelId}",
            request.ParcelId);

        var parcel =
            await parcelRepository.GetByIdAsync(
                request.ParcelId,
                ct);

        if (parcel is null)
        {
            return ParcelErrors.NotFound;
        }

        var jobId = processTopographyJob.Enqueue(parcel.Id, ct);

        await cacheService.SetAsync(
            $"parcel-status:{parcel.Id}",
            ParcelStatus.Processing,
            ct: ct);

        parcel.MarkAsProcessing();

        await parcelRepository.UpdateAsync(parcel, ct);

        return new SubmitTopographyJobResponse(
            jobId,
            parcel.Id,
            ParcelStatus.Queued.ToString(),
            DateTime.UtcNow);
    }
}