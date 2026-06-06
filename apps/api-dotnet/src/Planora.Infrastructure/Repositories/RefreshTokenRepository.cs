using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Repositories.DTOs;
using Planora.Infrastructure.Persistence.Contexts;
using Planora.Domain.Common;

namespace Planora.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly PlanoraDbContext _dbContext;

    public RefreshTokenRepository(PlanoraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshTokenInfo?> GetByTokenAsync(string token, CancellationToken ct)
    {
        var entity = await _dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token, ct);

        return entity is null
            ? null
            : new RefreshTokenInfo(entity.UserId, entity.Token, entity.ExpiresAt, entity.RevokedAt);
    }

    public async Task<string> CreateAsync(Guid userId, DateTime expiresAt, CancellationToken ct)
    {
        var token = GenerateToken();

        var entity = new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return token;
    }

    public async Task<string> RotateAsync(Guid userId, string oldToken, DateTime expiresAt, CancellationToken ct)
    {
        var entity = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == oldToken && x.UserId == userId, ct);

        if (entity is not null)
        {
            entity.RevokedAt = DateTime.UtcNow;
        }

        var newToken = GenerateToken();

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = newToken,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(ct);

        return newToken;
    }

    public async Task InvalidateAsync(Guid userId, CancellationToken ct)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
