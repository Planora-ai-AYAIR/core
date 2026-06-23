using MediatR;
using Planora.Application.Features.Auth.Errors;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.VerifyOtp;
public sealed class VerifyOtpHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtService jwtService,
    IOtpService otpService) : IRequestHandler<VerifyOtpCommand, Result<VerifyOtpResponse>>
{
    public async Task<Result<VerifyOtpResponse>> Handle(VerifyOtpCommand request, CancellationToken ct)
    {
        var user = await userRepository.FindByIdAsync(request.UserId, ct);
        if (user is null)
            return AuthErrors.UserNotFound;

        var isValid = await otpService.ValidateAsync(user.Id, OtpPurposes.EmailVerification, request.Otp, ct);
        if (!isValid)
            return AuthErrors.InvalidOtp;

        var confirmResult = await userRepository.SetEmailConfirmedAsync(user.Id, true, ct);
        if (!confirmResult.Succeeded)
            return Error.Validation("Auth.VerifyFailed", string.Join(" ", confirmResult.Errors));

        var roles = await userRepository.GetRolesAsync(user.Id, ct);
        var role = roles.FirstOrDefault() ?? AuthRoles.Client;
        var (accessToken, _) = jwtService.GenerateAccessToken(user.Id, user.Email, roles);

        var refreshExpiry = DateTime.UtcNow.Add(jwtService.GetRefreshTokenLifetime());
        var refreshToken = await refreshTokenRepository.CreateAsync(user.Id, refreshExpiry, ct);

        string FullName = $"{user.FirstName} {user.LastName}".Trim();

        return new VerifyOtpResponse(user.Id, user.Email, FullName, user.PhoneNumber, role, true, accessToken, refreshToken);
    }
}