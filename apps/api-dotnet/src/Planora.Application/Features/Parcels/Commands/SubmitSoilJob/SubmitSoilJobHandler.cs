using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Dtos;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Parcels.Dtos.SubmitSoilJob;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitSoilJob;

public sealed class SubmitSoilJobHandler(
    IParcelRepository parcelRepository,
    IHybridCacheService cacheService,
    IProcessSoilJob processSoilJob,
    ILogger<SubmitSoilJobHandler> logger)
    : IRequestHandler<SubmitSoilJobCommand, Result<SubmitSoilJobResponse>>
{
    public async Task<Result<SubmitSoilJobResponse>> Handle(SubmitSoilJobCommand request, CancellationToken ct)
    {
        logger.LogInformation("Submitting soil job for ParcelId {ParcelId}", request.ParcelId);

        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);

        if (parcel is null)
        {
            return ParcelErrors.NotFound;
        }

        var processSoilRequest = new ProccessSoilJobAiRequest(
            ParcelId: parcel.Id,
            BoundaryGeoJson: parcel.Boundary.ToGeoJson(),
            AreaHectares: parcel.AreaHectares,
            CentroidLatitude: parcel.Centroid.Y,
            CentroidLongitude: parcel.Centroid.X
        );

        var jobId = processSoilJob.Enqueue(processSoilRequest);

        await cacheService.SetAsync(
            $"parcel-status:{parcel.Id}",
            ParcelStatus.Processing,
            ct: ct);

        parcel.MarkAsProcessing();
        await parcelRepository.UpdateAsync(parcel, ct);

        return new SubmitSoilJobResponse(jobId, parcel.Id, ParcelStatus.Queued.ToString(), DateTime.UtcNow);
    }
}
