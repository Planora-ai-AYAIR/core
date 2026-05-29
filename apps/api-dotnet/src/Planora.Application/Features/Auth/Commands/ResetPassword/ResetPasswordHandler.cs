using MediatR;
using Planora.Application.Features.Auth;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IOtpService otpService) : IRequestHandler<ResetPasswordCommand, Response<ResetPasswordResponse>>
{
    public async Task<Response<ResetPasswordResponse>> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var handler = new ResponseHandler();

        var user = await userRepository.FindByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return handler.NotFound<ResetPasswordResponse>("User not found.");
        }

        var otpValid = await otpService.ValidateAsync(user.Id, OtpPurposes.PasswordReset, request.Otp, ct);
        if (!otpValid)
        {
            return handler.BadRequest<ResetPasswordResponse>("Invalid or expired OTP.");
        }

        var resetToken = await userRepository.GeneratePasswordResetTokenAsync(user.Id, ct);
        if (string.IsNullOrWhiteSpace(resetToken))
        {
            return handler.BadRequest<ResetPasswordResponse>("Unable to reset password.");
        }

        var resetResult = await userRepository.ResetPasswordAsync(user.Id, resetToken, request.NewPassword, ct);
        if (!resetResult.Succeeded)
        {
            return handler.BadRequest<ResetPasswordResponse>(string.Join(" ", resetResult.Errors));
        }

        await refreshTokenRepository.InvalidateAsync(user.Id, ct);

        var roles = await userRepository.GetRolesAsync(user.Id, ct);
        var role = roles.FirstOrDefault() ?? AuthRoles.Client;

        var response = new ResetPasswordResponse(user.Id, user.Email, user.PhoneNumber, role);
        return handler.Success(response, "Password reset successful.");
    }
}
