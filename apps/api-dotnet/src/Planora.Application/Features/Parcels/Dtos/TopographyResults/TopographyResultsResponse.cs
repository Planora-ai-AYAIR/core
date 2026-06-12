using System.Text.Json.Serialization;

namespace Planora.Application.Features.Parcels.Dtos.TopographyResults;

public record TopographyResultsResponse(
    Guid ParcelId,
    ElevationDto Elevation,
    SlopeAnalysisDto SlopeAnalysis,
    CutFillDto CutFill,
    ContourLinesDto ContourLines,
    PondingRiskDto PondingRisk,
    RasterTilesDto? RasterTiles, // nullable because `includeTiles` might be false
    DateTime GeneratedAt          // when the pipeline completed
);

public record ElevationDto(
    double Min,
    double Max,
    double Mean,
    string Unit
);

public record SlopeAnalysisDto(
    List<SlopeCategoryDto> Distribution
);

public record SlopeCategoryDto(
    string Category,
    string Range,
    double Percentage,
    string Color
);

public record CutFillDto(
    double CutVolume,
    double FillVolume,
    double NetVolume,
    string Unit
);

public record ContourLinesDto(
    string GeoJsonUrl,
    double Interval
);

public record PondingRiskDto(
    int ZonesCount,
    double TotalArea,
    string Unit,
    string GeoJsonUrl
);

public record RasterTilesDto(
    string Elevation,
    string Slope
);
