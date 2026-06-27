namespace Planora.Application.Common.Dtos;

public record ProccessBoreholeJobAiRequest(
    Guid ParcelId,
    string BoundaryGeoJson,
    decimal AreaHectares,
    double CentroidLatitude,
    double CentroidLongitude
);
