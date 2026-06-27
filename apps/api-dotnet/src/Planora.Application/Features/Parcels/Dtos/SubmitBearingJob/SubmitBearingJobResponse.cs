namespace Planora.Application.Features.Parcels.Dtos.SubmitBearingJob;

public sealed record SubmitBearingJobResponse(
    string JobId,
    Guid ParcelId,
    string Status,
    DateTime SubmittedAt);
