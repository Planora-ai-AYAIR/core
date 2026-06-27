namespace Planora.Application.Features.Parcels.Dtos.BoreholeResults;

public sealed record BoreholePlacementPointDto(
    string Id,
    double Latitude,
    double Longitude,
    string Priority,
    string? Reason = null,
    double? EstimatedDepth = null
);
