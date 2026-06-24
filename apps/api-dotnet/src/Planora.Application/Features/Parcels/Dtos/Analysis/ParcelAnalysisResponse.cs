namespace Planora.Application.Features.Parcels.Dtos.Analysis;

public sealed record ParcelAnalysisResponse(
    Guid ParcelId,
    TopographyAssetsDto? Topography,
    SoilAssetsDto? Soil,
    RiskAssetsDto? Risk,
    BoreholeAssetsDto? Borehole,

    // UTC timestamp at which all presigned URLs will expire (UtcNow + 1 hour).
    DateTime PresignedUrlsExpireAt);

public sealed record TopographyAssetsDto(
    string? ContourGeoJsonUrl,
    string? PondingGeoJsonUrl,
    string? ElevationTileUrl,
    string? SlopeTileUrl);

public sealed record SoilAssetsDto(
    string? HeatmapTileUrl);

public sealed record RiskAssetsDto(
    string? FloodGeoJsonUrl);

public sealed record BoreholeAssetsDto(
    string? PlacementGeoJsonUrl);
