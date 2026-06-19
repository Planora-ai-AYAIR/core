namespace Planora.Application.Features.Parcels.Dtos.SubmitRiskJob;

public sealed record SubmitRiskJobResponse(
    string JobId,
    Guid ParcelId,
    string Status,
    DateTime SubmittedAt);
