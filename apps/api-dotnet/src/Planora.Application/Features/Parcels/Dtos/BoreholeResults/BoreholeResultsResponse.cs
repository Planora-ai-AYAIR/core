namespace Planora.Application.Features.Parcels.Dtos.BoreholeResults;

public sealed record BoreholeResultsResponse(
    Guid ParcelId,
    int MinimumRequired,
    int OptimalCount,
    double CoveragePercentage,
    string? GridSize,
    string? PlacementStrategy,
    List<BoreholePlacementPointDto>? PlacementPoints,
    string? PlacementGeoJsonUrl,
    CostComparisonDto CostComparison,
    DateTime GeneratedAt
);
