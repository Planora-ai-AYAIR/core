using MediatR;
using Planora.Application.Features.Auth.Errors;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IOtpService otpService) : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResponse>>
{
    public async Task<Result<ResetPasswordResponse>> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var user = await userRepository.FindByIdAsync(request.UserId, ct);
        if (user is null)
            return AuthErrors.UserNotFound;

        var otpValid = await otpService.ValidateAsync(user.Id, OtpPurposes.PasswordReset, request.Otp, ct);
        if (!otpValid)
            return AuthErrors.InvalidOtp;

        var resetToken = await userRepository.GeneratePasswordResetTokenAsync(user.Id, ct);
        if (string.IsNullOrWhiteSpace(resetToken))
            return AuthErrors.PasswordResetFailed;

        var resetResult = await userRepository.ResetPasswordAsync(user.Id, resetToken, request.NewPassword, ct);
        if (!resetResult.Succeeded)
            return Error.Validation("Auth.ResetFailed", string.Join(" ", resetResult.Errors));

        await refreshTokenRepository.InvalidateAsync(user.Id, ct);

        var roles = await userRepository.GetRolesAsync(user.Id, ct);
        var role = roles.FirstOrDefault() ?? AuthRoles.Client;

        return new ResetPasswordResponse(user.Id, user.Email, user.PhoneNumber, role);
    }
}