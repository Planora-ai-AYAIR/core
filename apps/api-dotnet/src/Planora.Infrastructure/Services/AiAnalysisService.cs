using Planora.Application.Common.Dtos;
using Planora.Application.Interfaces.Services;
using Planora.Infrastructure.API;

namespace Planora.Infrastructure.Services;

public sealed class AiAnalysisService(IAiApiClient client) : IAiAnalysisService
{
    public async Task<string> ProccessTopographyAsync(ProccessTopographyJobAiRequest request, CancellationToken ct)
        => UnwrapJobId(await client.ProccessTopographyAsync(request, ct), "topography");

    public async Task<string> ProccessSoilAsync(ProccessSoilJobAiRequest request, CancellationToken ct)
        => UnwrapJobId(await client.ProccessSoilAsync(request, ct), "soil");

    public async Task<string> ProccessRiskAsync(ProccessRiskJobAiRequest request, CancellationToken ct)
        => UnwrapJobId(await client.ProccessRiskAsync(request, ct), "risk");

    public async Task<string> ProccessBoreholeAsync(ProccessBoreholeJobAiRequest request, CancellationToken ct)
        => UnwrapJobId(await client.ProccessBoreholeAsync(request, ct), "borehole");

    public async Task<string> ProccessBearingAsync(ProccessBearingJobAiRequest request, CancellationToken ct)
        => UnwrapJobId(await client.ProccessBearingAsync(request, ct), "bearing");

    public async Task<string> ProccessPdfAsync(ProccessPdfJobAiRequest request, CancellationToken ct)
        => UnwrapJobId(await client.ProccessPdfAsync(request, ct), "pdf");

    public async Task<string> SubmitAnalysisJobAsync(SubmitAiAnalysisJobRequest request, CancellationToken ct)
        => UnwrapJobId(await client.SubmitAnalysisJobAsync(request, ct), "analysis");

    private static string UnwrapJobId(AiResponseEnvelope<AiJobAccepted> envelope, string module)
    {
        if (envelope.Data is null || string.IsNullOrWhiteSpace(envelope.Data.PythonJobId))
        {
            var errSummary = envelope.Errors is { Count: > 0 }
                ? string.Join("; ", envelope.Errors.ConvertAll(e => $"{e.Field}:{e.Code}:{e.Message}"))
                : envelope.Message ?? "no error details";

            throw new InvalidOperationException(
                $"AI {module} job acceptance returned no pythonJobId (statusCode={envelope.StatusCode}): {errSummary}");
        }

        return envelope.Data.PythonJobId;
    }
}
