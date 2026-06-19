using Planora.Domain.Analysis;

namespace Planora.Application.Interfaces.Repositories;

public interface IBoreholeResultRepository
{
    Task AddAsync(BoreholeResult result, CancellationToken cancellationToken);
    Task<BoreholeResult?> GetByAnalysisJobIdAsync(Guid analysisJobId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken ct);
}
