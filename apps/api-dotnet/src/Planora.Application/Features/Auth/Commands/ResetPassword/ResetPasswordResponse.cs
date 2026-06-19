namespace Planora.Application.Features.Auth.Commands.ResetPassword;

public sealed record ResetPasswordResponse(
    Guid UserId,
    string Email,
    string? PhoneNumber,
    string Role);
