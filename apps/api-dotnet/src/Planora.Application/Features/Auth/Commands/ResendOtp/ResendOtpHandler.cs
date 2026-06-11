using MediatR;
using Planora.Application.Common.Helpers;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.ResendOtp;

public sealed class ResendOtpHandler(
    IUserRepository userRepository,
    IOtpService otpService,
    IBackgroundJobService backgroundJob) : IRequestHandler<ResendOtpCommand, Result<ResendOtpResponse>>
{
    public async Task<Result<ResendOtpResponse>> Handle(ResendOtpCommand request, CancellationToken ct)
    {
        var user = await userRepository.FindByIdAsync(request.UserId, ct);
        if (user is null)
            return AuthErrors.UserNotFound;

        if (user.EmailConfirmed)
            return AuthErrors.EmailAlreadyVerified;

        var otp = await otpService.GenerateAsync(user.Id, OtpPurposes.EmailVerification, TimeSpan.FromMinutes(10), ct);
        var displayName = EmailDisplayNameHelper.GetDisplayName(user.FirstName, user.LastName, user.Email);

        backgroundJob.Enqueue<IEmailService>(x => x.SendOtpAsync(user.Email, displayName, otp, "Verify your email", ct));

        return new ResendOtpResponse(user.Id, false);
    }
}