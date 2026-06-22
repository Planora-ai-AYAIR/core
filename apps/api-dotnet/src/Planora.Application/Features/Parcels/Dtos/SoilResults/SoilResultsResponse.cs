namespace Planora.Application.Features.Parcels.Dtos.SoilResults;

public sealed record SoilResultsResponse(
    Guid ParcelId,
    double SandPercent,
    double SiltPercent,
    double ClayPercent,
    string CompositionUnit,
    double BulkDensity,
    string BulkDensityUnit,
    double OrganicCarbon,
    string OrganicCarbonUnit,
    double Ph,
    string PrimaryType,
    string UsdaClass,
    double? AiConfidence,
    List<DepthProfileItem>? MultiDepthProfile,
    string? HeatmapTileUrl,
    DateTime GeneratedAt
);
