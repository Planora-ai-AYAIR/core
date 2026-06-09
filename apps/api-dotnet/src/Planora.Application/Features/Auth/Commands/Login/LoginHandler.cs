using MediatR;
using Planora.Application.Common.Helpers;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.Login;

public sealed class LoginHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtService jwtService,
    IOtpService otpService,
    IEmailService emailService,
    IAuditLogRepository auditLogRepository) : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userRepository.FindByEmailAsync(request.Email, ct);
        if (user is null)
            return AuthErrors.InvalidCredentials;

        var passwordValid = await userRepository.CheckPasswordAsync(user.Id, request.Password, ct);
        if (!passwordValid)
            return AuthErrors.InvalidCredentials;

        var roles = await userRepository.GetRolesAsync(user.Id, ct);
        var role = roles.FirstOrDefault() ?? AuthRoles.Client;

        if (!user.EmailConfirmed)
        {
            var otp = await otpService.GenerateAsync(user.Id, OtpPurposes.EmailVerification, TimeSpan.FromMinutes(10), ct);
            var displayName = EmailDisplayNameHelper.GetDisplayName(user.FirstName, user.LastName, user.Email);
            await emailService.SendOtpAsync(user.Email, displayName, otp, "Verify your email", ct);

            return AuthErrors.EmailNotConfirmed;
        }

        var (accessToken, _) = jwtService.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshExpiry = DateTime.UtcNow.Add(jwtService.GetRefreshTokenLifetime());
        var refreshToken = await refreshTokenRepository.CreateAsync(user.Id, refreshExpiry, ct);

        await auditLogRepository.LogAsync(user.Id, "Login", null, ct);

        return new LoginResponse(user.Id, user.Email, user.PhoneNumber, role, true, accessToken, refreshToken);
    }
}