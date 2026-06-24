using Planora.Domain.AnalysisJob;

namespace Planora.Application.Interfaces.Repositories;

public interface IAnalysisJobRepository
{
    Task<AnalysisJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AnalysisJob?> GetByPythonJobIdAsync(string pythonJobId, CancellationToken ct = default);
    Task<IReadOnlyList<AnalysisJob>> GetByParcelIdAsync(Guid parcelId, CancellationToken ct = default);
    Task<IReadOnlyList<AnalysisJob>> GetPendingJobsAsync(CancellationToken ct = default);
    Task AddAsync(AnalysisJob job, CancellationToken ct = default);
    Task UpdateAsync(AnalysisJob job, CancellationToken ct);
    Task<AnalysisJob?> GetLatestCompletedByParcelIdAsync(Guid parcelId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct);
}