namespace Planora.Application.Common.Dtos;
public record ProccessRiskJobAiRequest(
    Guid ParcelId,
    string BoundaryGeoJson,
    decimal AreaHectares,
    double CentroidLatitude,
    double CentroidLongitude
);