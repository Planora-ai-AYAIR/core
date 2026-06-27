namespace Planora.Application.Features.Parcels.Dtos.ParcelDetail;

public sealed record CoordinateDto(double Longitude, double Latitude);

public sealed record ParcelDetailResponse(
    Guid Id,
    string Name,
    decimal AreaHectares,
    string? Country,
    string? Governorate,
    string Status,
    string? GeojsonKey,
    DateTime CreatedAt,
    double? CentroidLatitude,
    double? CentroidLongitude,
    IReadOnlyList<CoordinateDto>? BoundaryCoordinates);