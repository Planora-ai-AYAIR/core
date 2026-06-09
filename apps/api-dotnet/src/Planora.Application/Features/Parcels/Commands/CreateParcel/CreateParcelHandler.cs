using MediatR;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Planora.Application.Common.Options;
using Planora.Application.Features.Parcels.Dtos.CreateParcel;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Parcels;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Commands.CreateParcel;

public sealed class CreateParcelHandler(
    IParcelRepository parcelRepository,
    IHybridCacheService cacheService,
    ILogger<CreateParcelHandler> logger) : IRequestHandler<CreateParcelCommand, Result<CreateParcelResponse>>
{
    public async Task<Result<CreateParcelResponse>> Handle(CreateParcelCommand request, CancellationToken ct)
    {
        logger.LogInformation(
            "Processing parcel creation for UserId: {UserId}, Name: {ParcelName}",
            request.UserId, request.Name);

        Geometry geometry;
        try
        {
            geometry = new GeoJsonReader().Read <Geometry > (request.GeoJson);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse GeoJSON for parcel {ParcelName}", request.Name);
            return ParcelErrors.InvalidBoundary;
        }

        if (geometry is not Polygon polygon || !polygon.IsValid)
        {
            logger.LogWarning("Invalid polygon geometry for parcel {ParcelName}", request.Name);
            return ParcelErrors.InvalidBoundary;
        }

        var areaHectares = request.AreaUnit.ToLowerInvariant() switch
        {
            "m2" => request.Area / 10_000m,
            "hectares" => request.Area,
            "acres" => request.Area * 0.404686m,
            _ => request.Area
        };

        var envelope = polygon.EnvelopeInternal;
        var bbox = new BoundingBoxDto(envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY);
        var bufferedBbox = CalculateBufferedBoundingBox(polygon, 500);

        logger.LogDebug(
            "Calculated bounding box for {ParcelName}: [{MinX}, {MinY}, {MaxX}, {MaxY}]",
            request.Name, bbox.MinX, bbox.MinY, bbox.MaxX, bbox.MaxY);

        var parcelResult = Parcel.Create(
            Guid.NewGuid(),
            request.UserId,
            request.Name.Trim(),
            polygon,
            areaHectares,
            polygon.Centroid);

        if (parcelResult.IsError)
        {
            logger.LogWarning(
                "Parcel domain validation failed for {ParcelName}: {ErrorCode}",
                request.Name, parcelResult.TopError.Code);
            return parcelResult.Errors;
        }

        await parcelRepository.AddAsync(parcelResult.Value, ct);

        var response = new CreateParcelResponse(
            parcelResult.Value.Id,
            parcelResult.Value.Name,
            bbox,
            bufferedBbox,
            parcelResult.Value.AreaHectares * 10_000m,
            parcelResult.Value.CreatedAt);

        await cacheService.SetAsync(
            $"parcels:{parcelResult.Value.Id}",
            response,
            new CacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(15),
                LocalCacheExpiration = TimeSpan.FromMinutes(5),
                Tags = new[] { "parcels", $"user:{request.UserId}" }
            },
            ct);

        logger.LogInformation(
            "Parcel {ParcelId} created successfully for User {UserId}",
            parcelResult.Value.Id, request.UserId);
        return response;
    }

    private static BoundingBoxDto CalculateBufferedBoundingBox(Polygon polygon, double bufferMeters)
    {
        var centroid = polygon.Centroid;
        var lat = centroid.Y;
        var metersPerDegreeLat = 111_000.0;
        var metersPerDegreeLon = 111_000.0 * Math.Cos(lat * Math.PI / 180.0);

        var env = polygon.EnvelopeInternal;
        return new BoundingBoxDto(
            env.MinX - bufferMeters / metersPerDegreeLon,
            env.MinY - bufferMeters / metersPerDegreeLat,
            env.MaxX + bufferMeters / metersPerDegreeLon,
            env.MaxY + bufferMeters / metersPerDegreeLat);
    }
}