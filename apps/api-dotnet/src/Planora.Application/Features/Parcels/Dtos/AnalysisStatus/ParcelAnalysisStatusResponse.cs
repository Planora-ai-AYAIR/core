namespace Planora.Application.Features.Parcels.Dtos.AnalysisStatus;

public sealed record ParcelAnalysisStatusResponse(
    Guid ParcelId,
    string Status,
    IReadOnlyList<ModuleStatusDto> Modules,
    DateTime? UpdatedAt);

public sealed record ModuleStatusDto(
    string Type,
    string Status,
    string? ErrorMessage,
    DateTime? CompletedAt);
