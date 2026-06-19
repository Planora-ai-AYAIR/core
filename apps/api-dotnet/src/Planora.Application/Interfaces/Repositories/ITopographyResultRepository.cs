using Planora.Domain.Entities;

namespace Planora.Application.Interfaces.Repositories;

public interface ITopographyResultRepository
{
    Task AddAsync(TopographyResult result, CancellationToken cancellationToken);
    Task<TopographyResult?> GetByAnalysisJobIdAsync(Guid analysisJobId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken ct);
}
