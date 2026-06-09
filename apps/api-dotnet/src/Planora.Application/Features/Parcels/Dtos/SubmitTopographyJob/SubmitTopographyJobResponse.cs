namespace Planora.Application.Features.Parcels.Dtos.SubmitTopographyJob
{
    public sealed record SubmitTopographyJobResponse(
        string JobId,
        Guid ParcelId,
        string Status,
        DateTime SubmittedAt);
}
