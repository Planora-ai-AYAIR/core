using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Parcels.Dtos.ParcelList;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetParcelList;

public sealed class GetParcelListHandler(
    IParcelRepository parcelRepository,
    ILogger<GetParcelListHandler> logger)
    : IRequestHandler<GetParcelListQuery, Result<ParcelListResponse>>
{
    public async Task<Result<ParcelListResponse>> Handle(
        GetParcelListQuery request,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Fetching parcel list for user {UserId}",
            request.UserId);

        var parcels = await parcelRepository.GetByUserIdAsync(
            request.UserId,
            ct);

        var summaries = parcels
            .Select(p => new ParcelSummaryDto(
                p.Id,
                p.Name,
                p.AreaHectares,
                p.Status.ToString(),
                p.CreatedAt,
                p.Centroid.Y,
                p.Centroid.X))
            .ToList();

        return new ParcelListResponse(summaries);
    }
}