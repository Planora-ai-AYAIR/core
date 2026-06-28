using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Analysis;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Persistence.Repositories;

public sealed class BearingResultRepository : IBearingResultRepository
{
    private readonly PlanoraDbContext _dbContext;

    public BearingResultRepository(PlanoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(BearingResult result, CancellationToken cancellationToken)
    {
        await _dbContext.Set<BearingResult>().AddAsync(result, cancellationToken);
    }

    public async Task<BearingResult?> GetByAnalysisJobIdAsync(Guid analysisJobId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<BearingResult>()
            .FirstOrDefaultAsync(b => b.AnalysisJobId == analysisJobId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
