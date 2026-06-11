using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Auth.Commands.ForgotPassword;
using Planora.Application.Features.Auth.Commands.Login;
using Planora.Application.Features.Auth.Commands.Logout;
using Planora.Application.Features.Auth.Commands.RefreshToken;
using Planora.Application.Features.Auth.Commands.Register;
using Planora.Application.Features.Auth.Commands.ResendOtp;
using Planora.Application.Features.Auth.Commands.ResetPassword;
using Planora.Application.Features.Auth.Commands.VerifyOtp;
using Planora.Application.Features.Auth.Dtos;
using Planora.Api.Helpers;
using Planora.Domain.Shared.Results; // Added to access the Error class
using ChangePasswordCommand = Planora.Application.Features.Auth.Commands.ChangePassword.ChangePasswordCommand;

namespace Planora.Api.Controllers;

[Route("api/[controller]")]
public sealed class AuthController : BaseApiController
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _sender.Send(command, ct);

        if (result.IsError) return Problem(result.Errors);

        return OkEnvelope(result.Value, "Login successful");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.PhoneNumber, request.FirstName, request.LastName);
        var result = await _sender.Send(command, ct);

        if (result.IsError) return Problem(result.Errors);

        return CreatedEnvelope(result.Value, "User registered successfully");
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken ct)
    {
        var command = new VerifyOtpCommand(request.UserId, request.Otp);
        var result = await _sender.Send(command, ct);

        if (result.IsError) return Problem(result.Errors);

        return OkEnvelope(result.Value, "OTP verified successfully");
    }

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request, CancellationToken ct)
    {
        var command = new ResendOtpCommand(request.UserId);
        var result = await _sender.Send(command, ct);

        if (result.IsError) return Problem(result.Errors);

        return OkEnvelope(result.Value, "OTP resent successfully");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var command = new ForgotPasswordCommand(request.Email, request.PhoneNumber);
        var result = await _sender.Send(command, ct);

        if (result.IsError) return Problem(result.Errors);

        return OkEnvelope(result.Value, "Password reset instructions sent");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var command = new ResetPasswordCommand(request.UserId, request.Otp, request.NewPassword, request.ConfirmPassword);
        var result = await _sender.Send(command, ct);

        if (result.IsError) return Problem(result.Errors);

        return OkEnvelope(result.Value, "Password has been reset successfully");
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await _sender.Send(command, ct);

        if (result.IsError) return Problem(result.Errors);

        return OkEnvelope(result.Value, "Token refreshed successfully");
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // Extract User ID from the JWT token claims
        if (!User.TryGetUserId(out var userId))
        {
            // Use Problem to ensure the response follows the unified API contract envelope
            return Problem([Error.Unauthorized("Auth.Unauthorized", "User is not authorized.")]);
        }

        var result = await _sender.Send(new LogoutCommand(userId), ct);

        if (result.IsError) return Problem(result.Errors);

        // Pass null since Logout doesn't return data, but cast to object? to satisfy generics
        return OkEnvelope<object?>(null, "Logged out successfully");
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        // Extract User ID from the JWT token claims
        if (!User.TryGetUserId(out var userId))
        {
            // Ensure 401 Unauthorized is wrapped in the standard envelope
            return Problem([Error.Unauthorized("Auth.Unauthorized", "User is not authorized.")]);
        }

        var result = await _sender.Send(
            new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword, request.ConfirmNewPassword),
            ct);

        if (result.IsError) return Problem(result.Errors);

        // Pass null since ChangePassword doesn't return data
        return OkEnvelope<object?>(null, "Password changed successfully");
    }
}