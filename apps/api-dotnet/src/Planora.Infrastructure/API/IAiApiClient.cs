using Planora.Application.Common.Dtos;
using Refit;

namespace Planora.Infrastructure.API;

public interface IAiApiClient
{
    [Post("/topography/jobs")]
    Task<AiResponseEnvelope<AiJobAccepted>> ProccessTopographyAsync(
        [Body] ProccessTopographyJobAiRequest request, CancellationToken ct);

    [Post("/soil/jobs")]
    Task<AiResponseEnvelope<AiJobAccepted>> ProccessSoilAsync(
        [Body] ProccessSoilJobAiRequest request, CancellationToken ct);

    [Post("/risks/jobs")]
    Task<AiResponseEnvelope<AiJobAccepted>> ProccessRiskAsync(
        [Body] ProccessRiskJobAiRequest request, CancellationToken ct);

    [Post("/boreholes/jobs")]
    Task<AiResponseEnvelope<AiJobAccepted>> ProccessBoreholeAsync(
        [Body] ProccessBoreholeJobAiRequest request, CancellationToken ct);

    // NOTE: Python §3 internal API does NOT yet expose a dedicated bearing endpoint.
    // This route is reserved; the AI team needs to add `POST /api/v1/bearings/jobs`
    // mirroring the per-module shape. See AI_TEAM_REQUIREMENTS.md.
    [Post("/bearings/jobs")]
    Task<AiResponseEnvelope<AiJobAccepted>> ProccessBearingAsync(
        [Body] ProccessBearingJobAiRequest request, CancellationToken ct);

    [Post("/reports/jobs")]
    Task<AiResponseEnvelope<AiJobAccepted>> ProccessPdfAsync(
        [Body] ProccessPdfJobAiRequest request, CancellationToken ct);

    [Post("/analysis/jobs")]
    Task<AiResponseEnvelope<AiJobAccepted>> SubmitAnalysisJobAsync(
        [Body] SubmitAiAnalysisJobRequest request, CancellationToken ct);
}
