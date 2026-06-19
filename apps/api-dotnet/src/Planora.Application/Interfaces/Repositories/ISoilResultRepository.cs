using Planora.Domain.Analysis;

namespace Planora.Application.Interfaces.Repositories;

public interface ISoilResultRepository
{
    Task AddAsync(SoilResult result, CancellationToken cancellationToken);
    Task<SoilResult?> GetByAnalysisJobIdAsync(Guid analysisJobId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken ct);
}
