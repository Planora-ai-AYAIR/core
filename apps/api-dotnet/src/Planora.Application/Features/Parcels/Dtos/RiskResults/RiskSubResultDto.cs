namespace Planora.Application.Features.Parcels.Dtos.RiskResults;

public sealed record RiskSubResultDto(
    int Score,
    string Level,
    List<string>? Factors = null,
    string? GeoJsonUrl = null,
    string? Source = null,
    double? ReplacementDepth = null,
    string? Susceptibility = null
);
