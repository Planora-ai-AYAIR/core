namespace Planora.Application.Features.Auth.Dtos;

public sealed record ResetPasswordRequest(
    Guid UserId,
    string Otp,
    string NewPassword,
    string ConfirmPassword);
