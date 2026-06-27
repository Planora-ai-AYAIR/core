namespace Planora.Application.Common.Dtos;
public record ProccessTopographyJobAiRequest(
    Guid ParcelId,
    string BoundaryGeoJson,
    decimal AreaHectares,
    double CentroidLatitude,
    double CentroidLongitude
);