using MediatR;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Auth;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.ResendOtp;

public sealed class ResendOtpHandler(
    IUserRepository userRepository,
    IOtpService otpService,
    IEmailService emailService) : IRequestHandler<ResendOtpCommand, Response<ResendOtpResponse>>
{
    public async Task<Response<ResendOtpResponse>> Handle(ResendOtpCommand request, CancellationToken ct)
    {
        var handler = new ResponseHandler();

        var user = await userRepository.FindByIdAsync(request.UserId, ct);
        if (user is null)
        {
            return handler.NotFound<ResendOtpResponse>("User not found.");
        }

        if (user.EmailConfirmed)
        {
            return handler.Success(new ResendOtpResponse(user.Id, true), "Email already verified.");
        }

        var otp = await otpService.GenerateAsync(user.Id, OtpPurposes.EmailVerification, TimeSpan.FromMinutes(10), ct);
        var displayName = EmailDisplayNameHelper.GetDisplayName(user.FirstName, user.LastName, user.Email);
        await emailService.SendOtpAsync(user.Email, displayName, otp, "Verify your email", ct);

        return handler.Success(new ResendOtpResponse(user.Id, false), "OTP resent.");
    }

}
