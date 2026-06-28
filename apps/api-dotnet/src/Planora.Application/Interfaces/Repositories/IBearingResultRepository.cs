using Planora.Domain.Analysis;

namespace Planora.Application.Interfaces.Repositories;

public interface IBearingResultRepository
{
    Task AddAsync(BearingResult result, CancellationToken cancellationToken);
    Task<BearingResult?> GetByAnalysisJobIdAsync(Guid analysisJobId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken ct);
}
