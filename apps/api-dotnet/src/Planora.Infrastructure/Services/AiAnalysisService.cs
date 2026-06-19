using Planora.Application.Common.Dtos;
using Planora.Application.Interfaces.Services;
using Planora.Infrastructure.API;

namespace Planora.Infrastructure.Services;

public sealed class AiAnalysisService(IAiApiClient client) : IAiAnalysisService
{
  public async Task<string> ProccessTopographyAsync(ProccessTopographyJobAiRequest request, CancellationToken ct)
    {
        var response = await client.ProccessTopographyAsync(request, ct);
        return response;
    }
}