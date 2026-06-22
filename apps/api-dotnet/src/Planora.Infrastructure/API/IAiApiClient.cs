using Planora.Application.Common.Dtos;
using Refit;

namespace Planora.Infrastructure.API;

public interface IAiApiClient
{
    [Post("/topography/jobs")]
    Task<string> ProccessTopographyAsync([Body] ProccessTopographyJobAiRequest request, CancellationToken ct);

    [Post("/soil/jobs")]
    Task<string> ProccessSoilAsync([Body] ProccessSoilJobAiRequest request, CancellationToken ct);

    [Post("/risks/jobs")]
    Task<string> ProccessRiskAsync([Body] ProccessRiskJobAiRequest request, CancellationToken ct);

    [Post("/boreholes/jobs")]
    Task<string> ProccessBoreholeAsync([Body] ProccessBoreholeJobAiRequest request, CancellationToken ct);

    [Post("/reports/jobs")]
    Task<string> ProccessPdfAsync([Body] ProccessPdfJobAiRequest request, CancellationToken ct);
}
