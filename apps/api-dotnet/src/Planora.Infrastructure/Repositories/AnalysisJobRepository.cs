using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.AnalysisJob;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Repositories;
public sealed class AnalysisJobRepository(PlanoraDbContext context) : IAnalysisJobRepository
{
  public async Task<AnalysisJob?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await context.AnalysisJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<AnalysisJob?> GetByPythonJobIdAsync(string pythonJobId, CancellationToken ct) =>
        await context.AnalysisJobs.FirstOrDefaultAsync(j => j.PythonJobId == pythonJobId, ct);

    public async Task<IReadOnlyList<AnalysisJob>> GetByParcelIdAsync(Guid parcelId, CancellationToken ct) =>
        await context.AnalysisJobs
            .Where(j => j.ParcelId == parcelId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AnalysisJob>> GetPendingJobsAsync(CancellationToken ct) =>
        await context.AnalysisJobs
            .Where(j => j.Status == AnalysisJobStatus.Pending || j.Status == AnalysisJobStatus.Running)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(AnalysisJob job, CancellationToken ct)
    {
        await context.AnalysisJobs.AddAsync(job, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AnalysisJob job, CancellationToken ct)
    {
        context.AnalysisJobs.Update(job);
        await context.SaveChangesAsync(ct);
    }
    
    public async Task SaveChangesAsync(CancellationToken ct) =>
        await context.SaveChangesAsync(ct);
}