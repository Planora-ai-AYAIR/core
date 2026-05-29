namespace Planora.Application.Features.Auth.Commands.ChangePassword;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
