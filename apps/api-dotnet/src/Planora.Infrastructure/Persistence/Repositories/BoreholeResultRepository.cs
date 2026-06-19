using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Analysis;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Persistence.Repositories;

public sealed class BoreholeResultRepository : IBoreholeResultRepository
{
    private readonly PlanoraDbContext _dbContext;

    public BoreholeResultRepository(PlanoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(BoreholeResult result, CancellationToken cancellationToken)
    {
        await _dbContext.Set<BoreholeResult>().AddAsync(result, cancellationToken);
    }

    public async Task<BoreholeResult?> GetByAnalysisJobIdAsync(Guid analysisJobId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<BoreholeResult>()
            .FirstOrDefaultAsync(b => b.AnalysisJobId == analysisJobId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
