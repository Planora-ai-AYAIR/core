namespace Planora.Application.Features.Analysis.Dtos.StartAnalysis;

public sealed record AnalysisOptionsDto(
    bool IncludeTopography = true,
    bool IncludeSoil = true,
    bool IncludeBearing = true,
    bool IncludeRisk = true,
    bool IncludeBorehole = true,
    decimal? ContourInterval = null,
    List<decimal>? SlopeCategories = null,
    string? ReferencePlane = null,
    List<string>? SoilDepths = null);
