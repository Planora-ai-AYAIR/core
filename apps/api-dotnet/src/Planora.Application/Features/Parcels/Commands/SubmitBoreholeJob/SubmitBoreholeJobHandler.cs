using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Dtos;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Parcels.Dtos.SubmitBoreholeJob;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitBoreholeJob;

public sealed class SubmitBoreholeJobHandler(
    IParcelRepository parcelRepository,
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
        {
            return ParcelErrors.NotFound;
        }

        var processBoreholeRequest = new ProccessBoreholeJobAiRequest(
            ParcelId: parcel.Id,
            BoundaryGeoJson: parcel.Boundary.ToGeoJson(),
            AreaHectares: parcel.AreaHectares,
            CentroidLatitude: parcel.Centroid.Y,
            CentroidLongitude: parcel.Centroid.X
        );

        var jobId = processBoreholeJob.Enqueue(processBoreholeRequest);

        await cacheService.SetAsync(
            $"parcel-status:{parcel.Id}",
            ParcelStatus.Processing,
            ct: ct);

        parcel.MarkAsProcessing();
        await parcelRepository.UpdateAsync(parcel, ct);

        return new SubmitBoreholeJobResponse(jobId, parcel.Id, ParcelStatus.Queued.ToString(), DateTime.UtcNow);
    }
}
