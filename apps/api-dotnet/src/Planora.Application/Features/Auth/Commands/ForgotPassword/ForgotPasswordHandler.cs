using MediatR;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Auth.Errors;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordHandler(
    IUserRepository userRepository,
    IOtpService otpService,
    IEmailService emailService) : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    public async Task<Result<ForgotPasswordResponse>> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = !string.IsNullOrWhiteSpace(request.Email)
            ? await userRepository.FindByEmailAsync(request.Email, ct)
            : null;

        if (user is null && !string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            user = await userRepository.FindByPhoneAsync(request.PhoneNumber, ct);
        }

        if (user is null)
            return AuthErrors.UserNotFound; 

        var otp = await otpService.GenerateAsync(user.Id, OtpPurposes.PasswordReset, TimeSpan.FromMinutes(10), ct);
        var displayName = EmailDisplayNameHelper.GetDisplayName(user.FirstName, user.LastName, user.Email);

        await emailService.SendOtpAsync(user.Email, displayName, otp, "Reset your password", ct);

        return new ForgotPasswordResponse(user.Id);
    }
}