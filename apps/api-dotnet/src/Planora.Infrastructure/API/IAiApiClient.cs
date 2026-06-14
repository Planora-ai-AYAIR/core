using Planora.Application.Common.Dtos;
using Refit;

namespace Planora.Infrastructure.API;

public interface IAiApiClient {
    
    [Post("/topography/jobs")]
    Task<string> ProccessTopographyAsync([Body] ProccessTopographyJobAiRequest request, CancellationToken ct);
}