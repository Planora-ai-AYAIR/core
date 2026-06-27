using MediatR;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Planora.Application.Features.Parcels.Dtos.ParcelDetail;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetParcelDetail;

public sealed class GetParcelDetailHandler(
    IParcelRepository parcelRepository,
    ILogger<GetParcelDetailHandler> logger)
    : IRequestHandler<GetParcelDetailQuery, Result<ParcelDetailResponse>>
{
    public async Task<Result<ParcelDetailResponse>> Handle(
        GetParcelDetailQuery request,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Fetching parcel detail for ParcelId: {ParcelId}",
            request.ParcelId);

        var parcel = await parcelRepository.GetByIdAsync(
            request.ParcelId,
            ct);

        if (parcel is null || parcel.UserId != request.UserId)
        {
            logger.LogWarning(
                "Parcel not found or access denied. ParcelId: {ParcelId}, UserId: {UserId}",
                request.ParcelId,
                request.UserId);

            return ParcelErrors.NotFound;
        }

        var boundaryCoordinates = parcel.Boundary?.ExteriorRing?.Coordinates
            .Where(c => IsValidCoordinate(c.X) && IsValidCoordinate(c.Y))
            .Select(c => new CoordinateDto(c.X, c.Y))
            .ToList();

        return new ParcelDetailResponse(
            parcel.Id,
            parcel.Name,
            parcel.AreaHectares,
            parcel.Country,
            parcel.Governorate,
            parcel.Status.ToString(),
            parcel.GeojsonKey,
            parcel.CreatedAt,
            GetLatitude(parcel.Centroid),
            GetLongitude(parcel.Centroid),
            boundaryCoordinates?.Count > 0 ? boundaryCoordinates : null);
    }

    private static double? GetLatitude(Point? centroid) =>
        centroid is not null && IsValidCoordinate(centroid.Y) ? centroid.Y : null;

    private static double? GetLongitude(Point? centroid) =>
        centroid is not null && IsValidCoordinate(centroid.X) ? centroid.X : null;

    private static bool IsValidCoordinate(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);
}