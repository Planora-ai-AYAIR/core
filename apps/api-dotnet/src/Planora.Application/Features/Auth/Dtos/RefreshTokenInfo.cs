namespace Planora.Application.Interfaces.Repositories.DTOs;

public sealed record RefreshTokenInfo(
    Guid UserId,
    string Token,
    DateTime ExpiresAt,
    DateTime? RevokedAt);
