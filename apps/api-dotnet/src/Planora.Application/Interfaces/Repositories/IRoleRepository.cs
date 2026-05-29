namespace Planora.Application.Interfaces.Repositories;

public interface IRoleRepository
{
    Task EnsureRoleExistsAsync(string role, CancellationToken ct);
}
