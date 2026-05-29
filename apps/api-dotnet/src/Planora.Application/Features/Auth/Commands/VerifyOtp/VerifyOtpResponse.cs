namespace Planora.Application.Features.Auth.Commands.VerifyOtp;

public sealed record VerifyOtpResponse(
    Guid Id,
    string Email,
    string? PhoneNumber,
    string Role,
    bool IsEmailConfirmed,
    string AccessToken,
    string RefreshToken);
