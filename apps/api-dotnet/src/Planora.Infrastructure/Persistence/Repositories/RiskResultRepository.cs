using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Entities;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Persistence.Repositories;

public sealed class RiskResultRepository : IRiskResultRepository
{
    private readonly PlanoraDbContext _dbContext;

    public RiskResultRepository(PlanoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(RiskResult result, CancellationToken cancellationToken)
    {
        await _dbContext.Set<RiskResult>().AddAsync(result, cancellationToken);
    }

    public async Task<RiskResult?> GetByAnalysisJobIdAsync(Guid analysisJobId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<RiskResult>()
            .FirstOrDefaultAsync(t => t.AnalysisJobId == analysisJobId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}