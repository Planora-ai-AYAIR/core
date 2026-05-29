using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Auth.Commands.ChangePassword;
using Planora.Application.Features.Auth.Commands.ForgotPassword;
using Planora.Application.Features.Auth.Commands.Login;
using Planora.Application.Features.Auth.Commands.Logout;
using Planora.Application.Features.Auth.Commands.RefreshToken;
using Planora.Application.Features.Auth.Commands.Register;
using Planora.Application.Features.Auth.Commands.ResendOtp;
using Planora.Application.Features.Auth.Commands.ResetPassword;
using Planora.Application.Features.Auth.Commands.VerifyOtp;
using Planora.Api.Helpers;

namespace Planora.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    private readonly ISender sender = sender;

  [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return this.ToActionResult(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return this.ToActionResult(result);
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return this.ToActionResult(result);
    }

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return this.ToActionResult(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return this.ToActionResult(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return this.ToActionResult(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(new LogoutCommand(userId), ct);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(
            new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword, request.ConfirmNewPassword),
            ct);

        return this.ToActionResult(result);
    }
}
