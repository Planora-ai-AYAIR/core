using Planora.Application.Interfaces.Repositories.DTOs;

namespace Planora.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshTokenInfo?> GetByTokenAsync(string token, CancellationToken ct);
    Task<string> CreateAsync(Guid userId, DateTime expiresAt, CancellationToken ct);
    Task<string> RotateAsync(Guid userId, string oldToken, DateTime expiresAt, CancellationToken ct);
    Task InvalidateAsync(Guid userId, CancellationToken ct);
}
