using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.AnalysisJob;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Repositories;
public sealed class AnalysisJobRepository : IAnalysisJobRepository
{
    private readonly PlanoraDbContext _context;

    public AnalysisJobRepository(PlanoraDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisJob?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _context.AnalysisJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<AnalysisJob?> GetByPythonJobIdAsync(string pythonJobId, CancellationToken ct) =>
        await _context.AnalysisJobs.FirstOrDefaultAsync(j => j.PythonJobId == pythonJobId, ct);

    public async Task<IReadOnlyList<AnalysisJob>> GetByParcelIdAsync(Guid parcelId, CancellationToken ct) =>
        await _context.AnalysisJobs
            .Where(j => j.ParcelId == parcelId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AnalysisJob>> GetPendingJobsAsync(CancellationToken ct) =>
        await _context.AnalysisJobs
            .Where(j => j.Status == AnalysisJobStatus.Pending || j.Status == AnalysisJobStatus.Running)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(AnalysisJob job, CancellationToken ct)
    {
        await _context.AnalysisJobs.AddAsync(job, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AnalysisJob job, CancellationToken ct)
    {
        _context.AnalysisJobs.Update(job);
        await _context.SaveChangesAsync(ct);
    }
}