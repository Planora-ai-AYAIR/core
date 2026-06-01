using System.Net;
using MediatR;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Auth;
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
    IAuditLogRepository auditLogRepository) : IRequestHandler<LoginCommand, Response<LoginResponse>>
{

  public async Task<Response<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var handler = new ResponseHandler();

        var user = await userRepository.FindByEmailAsync(request.Email, ct);
        if (user is null)
        {
            return new Response<LoginResponse>("Invalid credentials")
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Succeeded = false
            };
        }

        var passwordValid = await userRepository.CheckPasswordAsync(user.Id, request.Password, ct);
        if (!passwordValid)
        {
            return new Response<LoginResponse>("Invalid credentials")
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Succeeded = false
            };
        }

        var roles = await userRepository.GetRolesAsync(user.Id, ct);
        var role = roles.FirstOrDefault() ?? AuthRoles.Client;

        if (!user.EmailConfirmed)
        {
            var otp = await otpService.GenerateAsync(user.Id, OtpPurposes.EmailVerification, TimeSpan.FromMinutes(10), ct);
            var displayName = EmailDisplayNameHelper.GetDisplayName(user.FirstName, user.LastName, user.Email);
            await emailService.SendOtpAsync(user.Email, displayName, otp, "Verify your email", ct);

            var pendingResponse = new LoginResponse(
                user.Id,
                user.Email,
                user.PhoneNumber,
                role,
                false,
                null,
                null);

            return new Response<LoginResponse>(pendingResponse, "Email not confirmed. OTP sent.")
            {
                StatusCode = HttpStatusCode.Found
            };
        }

        var (accessToken, _) = jwtService.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshExpiry = DateTime.UtcNow.Add(jwtService.GetRefreshTokenLifetime());
        var refreshToken = await refreshTokenRepository.CreateAsync(user.Id, refreshExpiry, ct);

        await auditLogRepository.LogAsync(user.Id, "Login", null, ct);

        var response = new LoginResponse(
            user.Id,
            user.Email,
            user.PhoneNumber,
            role,
            true,
            accessToken,
            refreshToken);

        return handler.Success(response, "Login successful.");
    }

}
