using Planora.Domain.Entities;

namespace Planora.Application.Interfaces.Repositories;

public interface IRiskResultRepository
{
    Task AddAsync(RiskResult result, CancellationToken cancellationToken);
    Task<RiskResult?> GetByAnalysisJobIdAsync(Guid analysisJobId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken ct);
}
