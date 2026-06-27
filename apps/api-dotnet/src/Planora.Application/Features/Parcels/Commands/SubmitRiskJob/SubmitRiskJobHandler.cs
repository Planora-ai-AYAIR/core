using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Common.Dtos;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Parcels.Dtos.SubmitRiskJob;
using Planora.Application.Interfaces.Jobs;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Enums;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.SubmitRiskJob;

public sealed class SubmitRiskJobHandler(
    IParcelRepository parcelRepository,
    IHybridCacheService cacheService,
    IProcessRiskJob processRiskJob,
    ILogger<SubmitRiskJobHandler> logger)
    : IRequestHandler<SubmitRiskJobCommand, Result<SubmitRiskJobResponse>>
{
    public async Task<Result<SubmitRiskJobResponse>> Handle(SubmitRiskJobCommand request, CancellationToken ct)
    {
        logger.LogInformation("Submitting risk job for ParcelId {ParcelId}", request.ParcelId);

        var parcel = await parcelRepository.GetByIdAsync(request.ParcelId, ct);

        if (parcel is null)
        {
            return ParcelErrors.NotFound;
        }

        var processRiskRequest = new ProccessRiskJobAiRequest(
            ParcelId: parcel.Id,
            BoundaryGeoJson: parcel.Boundary.ToGeoJson(),
            AreaHectares: parcel.AreaHectares,
            CentroidLatitude: parcel.Centroid.Y,
            CentroidLongitude: parcel.Centroid.X
        );

        var jobId = processRiskJob.Enqueue(processRiskRequest);

        await cacheService.SetAsync(
            $"parcel-status:{parcel.Id}",
            ParcelStatus.Processing,
            ct: ct);

        parcel.MarkAsProcessing();
        await parcelRepository.UpdateAsync(parcel, ct);

        return new SubmitRiskJobResponse(jobId, parcel.Id, ParcelStatus.Queued.ToString(), DateTime.UtcNow);
    }
}
