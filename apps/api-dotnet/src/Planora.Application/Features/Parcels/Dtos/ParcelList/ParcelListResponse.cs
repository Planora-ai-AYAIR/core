namespace Planora.Application.Features.Parcels.Dtos.ParcelList;

public sealed record ParcelSummaryDto(
    Guid Id,
    string Name,
    decimal AreaHectares,
    string Status,
    DateTime CreatedAt,
    double? CentroidLatitude,
    double? CentroidLongitude);

public sealed record ParcelListResponse(
    IReadOnlyList<ParcelSummaryDto> Parcels);