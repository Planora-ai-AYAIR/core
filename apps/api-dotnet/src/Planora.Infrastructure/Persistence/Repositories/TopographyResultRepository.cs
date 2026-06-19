using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Entities;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Persistence.Repositories;

public sealed class TopographyResultRepository : ITopographyResultRepository
{
    private readonly PlanoraDbContext _dbContext;

    public TopographyResultRepository(PlanoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TopographyResult result, CancellationToken cancellationToken)
    {
        await _dbContext.Set<TopographyResult>().AddAsync(result, cancellationToken);
    }

    public async Task<TopographyResult?> GetByAnalysisJobIdAsync(Guid analysisJobId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<TopographyResult>()
            .FirstOrDefaultAsync(t => t.AnalysisJobId == analysisJobId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
