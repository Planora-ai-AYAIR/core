namespace Planora.Application.Features.Parcels.Dtos.SubmitPdfJob;

public sealed record SubmitPdfJobResponse(
    string JobId,
    Guid ParcelId,
    string Status,
    DateTime SubmittedAt);
