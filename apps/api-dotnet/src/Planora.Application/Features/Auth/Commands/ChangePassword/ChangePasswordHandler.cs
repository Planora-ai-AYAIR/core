using MediatR;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository) : IRequestHandler<ChangePasswordCommand, Result<ChangePasswordResponse>>
{
    public async Task<Result<ChangePasswordResponse>> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var result = await userRepository.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword, ct);

        if (!result.Succeeded)
            return Error.Validation("Auth.ChangePasswordFailed", string.Join(" ", result.Errors));

        await refreshTokenRepository.InvalidateAsync(request.UserId, ct);

        return new ChangePasswordResponse(request.UserId);
    }
}