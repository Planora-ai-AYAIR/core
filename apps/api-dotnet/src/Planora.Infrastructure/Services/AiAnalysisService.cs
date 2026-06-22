using Planora.Application.Common.Dtos;
using Planora.Application.Interfaces.Services;
using Planora.Infrastructure.API;

namespace Planora.Infrastructure.Services;

public sealed class AiAnalysisService(IAiApiClient client) : IAiAnalysisService
{
    public async Task<string> ProccessTopographyAsync(ProccessTopographyJobAiRequest request, CancellationToken ct)
    {
        return await client.ProccessTopographyAsync(request, ct);
    }

    public async Task<string> ProccessSoilAsync(ProccessSoilJobAiRequest request, CancellationToken ct)
    {
        return await client.ProccessSoilAsync(request, ct);
    }

    public async Task<string> ProccessRiskAsync(ProccessRiskJobAiRequest request, CancellationToken ct)
    {
        return await client.ProccessRiskAsync(request, ct);
    }

    public async Task<string> ProccessBoreholeAsync(ProccessBoreholeJobAiRequest request, CancellationToken ct)
    {
        return await client.ProccessBoreholeAsync(request, ct);
    }

    public async Task<string> ProccessPdfAsync(ProccessPdfJobAiRequest request, CancellationToken ct)
    {
        return await client.ProccessPdfAsync(request, ct);
    }
}
