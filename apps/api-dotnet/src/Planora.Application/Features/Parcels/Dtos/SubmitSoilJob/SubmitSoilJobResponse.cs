namespace Planora.Application.Features.Parcels.Dtos.SubmitSoilJob;

public sealed record SubmitSoilJobResponse(
    string JobId,
    Guid ParcelId,
    string Status,
    DateTime SubmittedAt);
