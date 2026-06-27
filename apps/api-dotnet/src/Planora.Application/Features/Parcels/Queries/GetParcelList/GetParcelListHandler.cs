using System.Linq;
using MediatR;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
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
                GetLatitude(p.Centroid),
                GetLongitude(p.Centroid)))
            .ToList();

        return new ParcelListResponse(summaries);
    }

    private static double? GetLatitude(Point? centroid) =>
        centroid is not null && IsValidCoordinate(centroid.Y) ? centroid.Y : null;

    private static double? GetLongitude(Point? centroid) =>
        centroid is not null && IsValidCoordinate(centroid.X) ? centroid.X : null;

    private static bool IsValidCoordinate(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);
}