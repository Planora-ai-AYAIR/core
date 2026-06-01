namespace Planora.Application.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task LogAsync(Guid? userId, string action, string? metadata, CancellationToken ct);
}
