namespace Planora.Application.Features.Parcels.Dtos.RefreshAssets;

public sealed record RefreshAssetsResponse(
    Guid ParcelId,
    DateTime PresignedUrlsExpireAt,
    RefreshMapLayersDto MapLayers);

public sealed record RefreshMapLayersDto(
    string ContourGeoJsonUrl,
    string PondingGeoJsonUrl,
    string ElevationTileUrl,
    string SlopeTileUrl,
    string SoilHeatmapTileUrl,
    string SoilTypeGeoJsonUrl,
    string FloodRiskZonesGeoJsonUrl,
    string RiskHeatmapTileUrl,
    string BoreholePointsGeoJsonUrl);
