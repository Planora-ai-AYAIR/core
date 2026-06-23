using MediatR;
using Planora.Application.Features.Auth.Errors;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtService jwtService) : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var tokenInfo = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);
        if (tokenInfo is null || tokenInfo.RevokedAt is not null || tokenInfo.ExpiresAt <= DateTime.UtcNow)
            return AuthErrors.InvalidRefreshToken;

        var user = await userRepository.FindByIdAsync(tokenInfo.UserId, ct);
        if (user is null)
            return AuthErrors.InvalidRefreshToken; 

        var roles = await userRepository.GetRolesAsync(user.Id, ct);
        var (accessToken, _) = jwtService.GenerateAccessToken(user.Id, user.Email, roles);

        var refreshExpiry = DateTime.UtcNow.Add(jwtService.GetRefreshTokenLifetime());
        var newRefreshToken = await refreshTokenRepository.RotateAsync(user.Id, request.RefreshToken, refreshExpiry, ct);

        return new RefreshTokenResponse(accessToken, newRefreshToken);
    }
}