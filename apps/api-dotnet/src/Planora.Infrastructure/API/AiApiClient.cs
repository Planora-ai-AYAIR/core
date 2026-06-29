using System.Net.Http.Json;
using System.Text.Json;
using Planora.Application.Common.Dtos;

namespace Planora.Infrastructure.API;

public sealed class AiApiClient : IAiApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AiApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> ProccessTopographyAsync(ProccessTopographyJobAiRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("/topography/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> ProccessSoilAsync(ProccessSoilJobAiRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("/soil/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> ProccessRiskAsync(ProccessRiskJobAiRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("/risks/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> ProccessBoreholeAsync(ProccessBoreholeJobAiRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("/boreholes/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> ProccessPdfAsync(ProccessPdfJobAiRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("/reports/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<AiAnalysisJobResponse> SubmitAnalysisJobAsync(SubmitAiAnalysisJobRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("/analysis/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AiAnalysisJobResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}