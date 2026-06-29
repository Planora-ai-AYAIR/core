using System.Net.Http.Json;
using System.Text.Json;
using Planora.Application.Common.Dtos;
using Planora.Infrastructure.API;

public sealed class AiApiClient : IAiApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string ApiV1 = "/api/v1";

    public AiApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> ProccessTopographyAsync(ProccessTopographyJobAiRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync($"{ApiV1}/topography/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> ProccessSoilAsync(ProccessSoilJobAiRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync($"{ApiV1}/soil/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> ProccessRiskAsync(ProccessRiskJobAiRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync($"{ApiV1}/risks/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> ProccessBoreholeAsync(ProccessBoreholeJobAiRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync($"{ApiV1}/boreholes/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> ProccessPdfAsync(ProccessPdfJobAiRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync($"{ApiV1}/reports/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<AiAnalysisJobResponse> SubmitAnalysisJobAsync(SubmitAiAnalysisJobRequest request, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync($"{ApiV1}/analysis/jobs", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AiAnalysisJobResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}