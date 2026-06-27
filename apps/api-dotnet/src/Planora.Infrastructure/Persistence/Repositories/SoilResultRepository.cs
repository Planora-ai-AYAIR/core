using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Analysis;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Persistence.Repositories;

public sealed class SoilResultRepository : ISoilResultRepository
{
    private readonly PlanoraDbContext _dbContext;

    public SoilResultRepository(PlanoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SoilResult result, CancellationToken cancellationToken)
    {
        await _dbContext.Set<SoilResult>().AddAsync(result, cancellationToken);
    }

    public async Task<SoilResult?> GetByAnalysisJobIdAsync(Guid analysisJobId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<SoilResult>()
            .FirstOrDefaultAsync(t => t.AnalysisJobId == analysisJobId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}