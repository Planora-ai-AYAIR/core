using Planora.Application.Common.Dtos;

namespace Planora.Application.Interfaces.Services;

public interface IAiAnalysisService
{
    Task<string> ProccessTopographyAsync(ProccessTopographyJobAiRequest request, CancellationToken ct = default);
    Task<string> ProccessSoilAsync(ProccessSoilJobAiRequest request, CancellationToken ct = default);
    Task<string> ProccessRiskAsync(ProccessRiskJobAiRequest request, CancellationToken ct = default);
    Task<string> ProccessBoreholeAsync(ProccessBoreholeJobAiRequest request, CancellationToken ct = default);
    Task<string> ProccessPdfAsync(ProccessPdfJobAiRequest request, CancellationToken ct = default);
    Task<AiAnalysisJobResponse> SubmitAnalysisJobAsync(SubmitAiAnalysisJobRequest request, CancellationToken ct = default);
}
