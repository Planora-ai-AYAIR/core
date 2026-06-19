using Planora.Domain.AnalysisJob;

namespace Planora.Application.Interfaces.Repositories;

public interface IAnalysisJobRepository
{
    Task<AnalysisJob?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<AnalysisJob?> GetByPythonJobIdAsync(string pythonJobId, CancellationToken ct);
    Task<IReadOnlyList<AnalysisJob>> GetByParcelIdAsync(Guid parcelId, CancellationToken ct);
    Task<IReadOnlyList<AnalysisJob>> GetPendingJobsAsync(CancellationToken ct);
    Task AddAsync(AnalysisJob job, CancellationToken ct);
    Task UpdateAsync(AnalysisJob job, CancellationToken ct);
}