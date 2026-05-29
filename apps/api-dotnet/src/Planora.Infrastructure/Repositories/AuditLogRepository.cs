using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Entities;
using Planora.Infrastructure.Persistence.Contexts;

namespace Planora.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly PlanoraDbContext _dbContext;

    public AuditLogRepository(PlanoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(Guid? userId, string action, string? metadata, CancellationToken ct)
    {
        var entry = new AuthAuditLog
        {
            UserId = userId,
            Action = action,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AuthAuditLogs.Add(entry);
        await _dbContext.SaveChangesAsync(ct);
    }
}
