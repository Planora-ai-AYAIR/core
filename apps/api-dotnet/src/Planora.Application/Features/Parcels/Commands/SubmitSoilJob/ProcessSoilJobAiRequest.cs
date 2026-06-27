namespace Planora.Application.Common.Dtos;
public record ProccessSoilJobAiRequest(
    Guid ParcelId,
    string BoundaryGeoJson,
    decimal AreaHectares,
    double CentroidLatitude,
    double CentroidLongitude
);