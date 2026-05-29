using System.Net;
using MediatR;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtService jwtService) : IRequestHandler<RefreshTokenCommand, Response<RefreshTokenResponse>>
{
    public async Task<Response<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var tokenInfo = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);
        if (tokenInfo is null || tokenInfo.RevokedAt is not null || tokenInfo.ExpiresAt <= DateTime.UtcNow)
        {
            return new Response<RefreshTokenResponse>("Invalid refresh token")
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Succeeded = false
            };
        }

        var user = await userRepository.FindByIdAsync(tokenInfo.UserId, ct);
        if (user is null)
        {
            return new Response<RefreshTokenResponse>("Invalid refresh token")
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Succeeded = false
            };
        }

        var roles = await userRepository.GetRolesAsync(user.Id, ct);
        var (accessToken, _) = jwtService.GenerateAccessToken(user.Id, user.Email, roles);

        var refreshExpiry = DateTime.UtcNow.Add(jwtService.GetRefreshTokenLifetime());
        var newRefreshToken = await refreshTokenRepository.RotateAsync(user.Id, request.RefreshToken, refreshExpiry, ct);

        return new ResponseHandler().Success(
            new RefreshTokenResponse(accessToken, newRefreshToken),
            "Token refreshed.");
    }
}
