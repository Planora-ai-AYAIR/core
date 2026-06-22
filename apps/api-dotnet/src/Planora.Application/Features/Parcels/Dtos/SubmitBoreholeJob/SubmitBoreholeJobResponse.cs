namespace Planora.Application.Features.Parcels.Dtos.SubmitBoreholeJob;

public sealed record SubmitBoreholeJobResponse(
    string JobId,
    Guid ParcelId,
    string Status,
    DateTime SubmittedAt);
