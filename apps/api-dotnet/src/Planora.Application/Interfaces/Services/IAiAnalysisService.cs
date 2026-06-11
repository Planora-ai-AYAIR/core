using Planora.Application.Common.Dtos;

namespace Planora.Application.Interfaces.Services;

public interface IAiAnalysisService
{
  Task<string> ProccessTopographyAsync(ProccessTopographyJobAiRequest request, CancellationToken ct = default);
}