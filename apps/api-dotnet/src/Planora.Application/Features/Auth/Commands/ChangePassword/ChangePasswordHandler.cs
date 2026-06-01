using MediatR;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository) : IRequestHandler<ChangePasswordCommand, Response<ChangePasswordResponse>>
{
  public async Task<Response<ChangePasswordResponse>> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var handler = new ResponseHandler();

        var result = await userRepository.ChangePasswordAsync(
            request.UserId,
            request.CurrentPassword,
            request.NewPassword,
            ct);

        if (!result.Succeeded)
        {
            return handler.BadRequest<ChangePasswordResponse>(string.Join(" ", result.Errors));
        }

        await refreshTokenRepository.InvalidateAsync(request.UserId, ct);

        return handler.Success(new ChangePasswordResponse(request.UserId), "Password changed.");
    }
}
